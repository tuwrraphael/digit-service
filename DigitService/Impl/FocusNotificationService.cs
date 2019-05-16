using ButlerClient;
using Digit.Abstractions.Service;
using Digit.Focus;
using Digit.Focus.Model;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitPushService.Client;
using DigitService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class FocusNotificationService : IFocusSubscriber, IFocusNotificationService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _notifySempahores = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly IFocusStore _focusStore;
        private readonly IDigitLogger _logger;
        private readonly IButler _butler;
        private readonly IDigitPushServiceClient _digitPushServiceClient;
        private readonly IFocusExternalDataService _focusExternalDataService;
        private readonly DigitServiceOptions options;

        public FocusNotificationService(IFocusStore focusStore,
            IDigitLogger logger,
            IButler butler,
            IOptions<DigitServiceOptions> optionsAccessor,
            IDigitPushServiceClient digitPushServiceClient,
            IFocusExternalDataService focusExternalDataService)
        {
            _focusStore = focusStore;
            _logger = logger;
            _butler = butler;
            _digitPushServiceClient = digitPushServiceClient;
            _focusExternalDataService = focusExternalDataService;
            options = optionsAccessor.Value;
        }

        public async Task ActiveItemChanged(string userId, FocusItemWithExternalData currentItem)
        {

        }

        public async Task ActiveItemsChanged(string userId, FocusManageResult result)
        {
            foreach (var item in result.ActiveItems)
            {
                await NotifyOrInstall(userId, item);
            }
        }

        public async Task Notify(NotifyUserRequest request)
        {
            var item = await _focusExternalDataService.Get(request.UserId, request.FocusItemId);
            if (null == item)
            {
                return;
            }
            await NotifyOrInstall(request.UserId, item);
        }

        private string Minutes(DateTimeOffset t)
        {
            var m = (t - DateTime.Now).TotalMinutes;
            return $"{m:0} {(m == 1 ? "Minute" : "Minuten")}";
        }

        private string NotificationBody(FocusItemWithExternalData item)
        {
            var leave = $"Geh in {Minutes(item.IndicateTime)} los.";
            if (null != item.Directions)
            {
                var step = item.Directions.Routes[item.DirectionsMetadata.PeferredRoute].Steps.FirstOrDefault();
                if (null != step)
                {
                    return $"{leave} {step.Line.ShortName} kommt in {Minutes(step.DepartureTime)}.";
                }
            }
            if (null != item.CalendarEvent)
            {
                return $"{leave} {item.CalendarEvent.Subject} beginnt in {Minutes(item.CalendarEvent.Start)}.";
            }
            return leave;
        }

        private async Task NotifyOrInstall(string userId, FocusItemWithExternalData item)
        {
            var timeToDeparture = item.IndicateTime - DateTimeOffset.Now;
            if (timeToDeparture < FocusConstants.NotifyTime.Add(FocusConstants.ButlerInaccuracy))
            {
                var notifySemaphore = _notifySempahores.GetOrAdd(userId, s => new SemaphoreSlim(1));
                await notifySemaphore.WaitAsync();
                try
                {
                    if (!await _focusStore.FocusItemNotifiedAsync(item.Id))
                    {
                        try
                        {
                            await _digitPushServiceClient.Push[userId].Create(new DigitPushService.Models.PushRequest()
                            {
                                ChannelOptions = new Dictionary<string, string>() { { "digit.notify", null } },
                                Payload = JsonConvert.SerializeObject(new
                                {
                                    notification = new
                                    {
                                        tag = item.Id,
                                        title = item.CalendarEvent == null ? "Losgehen" : $"Losgehen zu {item.CalendarEvent.Subject}",
                                        body = NotificationBody(item)
                                    }
                                })
                            });
                        }
                        catch (Exception e)
                        {
                            await _logger.Log(userId, $"Could notify user ({e.Message}).", 3);
                        }
                        await _focusStore.SetFocusItemNotifiedAsync(item.Id); // always set notified for now to prevent massive notification spam
                    }
                }
                finally
                {
                    notifySemaphore.Release();
                }
            }
            else
            {
                // plan the notification anyways, even if the location might be updated, in case no update is received (low phone battery for example)
                // it will be ignored if any of the conditions (user location, traffic, event start time) changed
                await _butler.InstallAsync(new WebhookRequest()
                {
                    When = item.IndicateTime.Add(-FocusConstants.NotifyTime).UtcDateTime,
                    Data = new NotifyUserRequest()
                    {
                        UserId = userId,
                        FocusItemId = item.Id
                    },
                    Url = options.NotifyUserCallbackUri
                });
            }
        }
    }
}
