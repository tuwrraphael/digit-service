using ButlerClient;
using CalendarService.Client;
using CalendarService.Models;
using Digit.Focus.Service;
using DigitPushService.Client;
using DigitService.Models;
using DigitService.Service;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TravelService.Client;
using TravelService.Models;
using TravelService.Models.Directions;

namespace DigitService.Controllers
{

    public class FocusService : IFocusService
    {
        private readonly IDigitLogger logger;
        private readonly ICalendarServiceClient calendarServiceClient;
        private readonly ITravelServiceClient travelServiceClient;
        private readonly IButler butler;
        private readonly IDigitPushServiceClient digitPushServiceClient;
        private readonly DigitServiceOptions options;
        private readonly IFocusStore focusStore;
        private readonly IFocusCalendarSyncService focusCalendarSyncService;
        private readonly ILocationService locationService;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _notifySempahores = new ConcurrentDictionary<string, SemaphoreSlim>();

        private static readonly TimeSpan NotifyTime = new TimeSpan(0, 6, 0);
        private static readonly TimeSpan ButlerInaccuracy = new TimeSpan(0, 1, 0);


        private static readonly TimeSpan DefaultTravelTime = new TimeSpan(0, 45, 0);

        public FocusService(IDigitLogger logger,
            ICalendarServiceClient calendarServiceClient, ITravelServiceClient travelServiceClient, IButler butler,
            IDigitPushServiceClient digitPushServiceClient,
            IFocusStore focusStore, IOptions<DigitServiceOptions> optionsAccessor,
            IFocusCalendarSyncService focusCalendarSyncService,
            ILocationService locationService)
        {
            this.logger = logger;
            this.calendarServiceClient = calendarServiceClient;
            this.travelServiceClient = travelServiceClient;
            this.butler = butler;
            this.digitPushServiceClient = digitPushServiceClient;
            options = optionsAccessor.Value;
            this.focusStore = focusStore;
            this.focusCalendarSyncService = focusCalendarSyncService;
            this.locationService = locationService;
        }


        public async Task NotifyCallbackAsync(NotifyUserRequest request)
        {
            await ManageFocus(request.UserId, await locationService.GetLastLocationAsync(request.UserId));
        }

        private Route SelectRoute(DirectionsResult directionsResult)
        {
            if (null != directionsResult?.TransitDirections && directionsResult.TransitDirections.Routes.Any())
            {
                if (directionsResult.TransitDirections.Routes.Where(v => v.DepatureTime.HasValue).Any())
                {
                    var route = directionsResult.TransitDirections.Routes.Where(v => v.DepatureTime.HasValue).First();
                    return route;
                }
            }
            return null;
        }

        private async Task<FocusManageResult> ManageFocus(string userId, Location location)
        {
            var res = new FocusManageResult();
            var activeItems = await focusStore.GetActiveAsync(userId);
            foreach (var focusItem in activeItems)
            {
                var evt = await calendarServiceClient.Users[userId].Feeds[focusItem.CalendarEventFeedId].Events.Get(focusItem.CalendarEventId);
                Route route = null;
                string directionsKey = null;
                var address = evt.GetFormattedAddress();
                if (null != address && null != location)
                {
                    try
                    {
                        var start = new Coordinate()
                        {
                            Lat = location.Latitude,
                            Lng = location.Longitude
                        };
                        bool requestWithNow = evt.Start <= DateTimeOffset.Now;
                        DirectionsResult directionsResult = null;
                        if (!requestWithNow)
                        {
                            directionsResult = await travelServiceClient.Directions.Transit.Get(start, address, evt.Start);
                            route = SelectRoute(directionsResult);
                            if (null != route && route.DepatureTime.Value < DateTimeOffset.Now)
                            {
                                requestWithNow = true;
                            }
                        }
                        if (requestWithNow)
                        {
                            directionsResult = await travelServiceClient.Directions.Transit.Get(start, address, null, DateTimeOffset.Now);
                            route = SelectRoute(directionsResult);
                        }
                        directionsKey = directionsResult?.CacheKey;
                    }
                    catch (TravelServiceException ex)
                    {
                        await logger.Log(userId, $"Error while retrieving directions for {evt.Subject}: {ex.Message}", 3);
                    }
                }
                DateTimeOffset departureTime;
                if (null == route)
                {
                    await logger.Log(userId, $"No departure time found, using {DefaultTravelTime.TotalMinutes:0} minutes for {evt.Subject}");
                    departureTime = evt.Start - DefaultTravelTime;
                }
                else
                {
                    departureTime = route.DepatureTime.Value;
                    res.Departures.Add(new FocusDeparture()
                    {
                        DepartureTime = departureTime,
                        Route = route,
                        Event = evt
                    });
                }
                await focusStore.UpdateWithDirections(focusItem.Id, departureTime, directionsKey);
                var timeToDeparture = departureTime - DateTimeOffset.Now;
                if (timeToDeparture < NotifyTime.Add(ButlerInaccuracy))
                {
                    var notifySemaphore = _notifySempahores.GetOrAdd(userId, s => new SemaphoreSlim(1));
                    await notifySemaphore.WaitAsync();
                    try
                    {
                        if (!await focusStore.FocusItemNotifiedAsync(focusItem.Id))
                        {
                            try
                            {
                                await digitPushServiceClient.Push[userId].Create(new DigitPushService.Models.PushRequest()
                                {
                                    ChannelOptions = new Dictionary<string, string>() { { "digit.notify", null } },
                                    Payload = JsonConvert.SerializeObject(new
                                    {
                                        notification = new
                                        {
                                            title = $"Losgehen zu {evt.Subject}",
                                            body = $"Mach dich auf den Weg. {evt.Subject} beginnt in {(evt.Start - DateTime.Now).TotalMinutes:0} Minuten."
                                        }
                                    })
                                });
                            }
                            catch (Exception e)
                            {
                                await logger.Log(userId, $"Could notify user ({e.Message}).", 3);
                            }
                            await focusStore.SetFocusItemNotifiedAsync(focusItem.Id); // always set notified for now to prevent massive notification spam
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
                    await butler.InstallAsync(new WebhookRequest()
                    {
                        When = departureTime.Add(-NotifyTime).UtcDateTime,
                        Data = new NotifyUserRequest()
                        {
                            UserId = userId,
                            FocusItemId = focusItem.Id
                        },
                        Url = options.NotifyUserCallbackUri
                    });
                }
            }
            return res;
        }

        public async Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location)
        {
            await logger.Log(userId, $"Received Location");     
            var manageResult = await ManageFocus(userId, location);
            return await locationService.LocationUpdateReceivedAsync(userId, location, DateTimeOffset.Now, manageResult);
        }

        public async Task ReminderDeliveryAsync(string userId, ReminderDelivery reminderDelivery)
        {
            if (!reminderDelivery.Removed)
            {
                await logger.Log(userId, $"Received reminder for {reminderDelivery.Event.Subject}");
            }
            else
            {
                await logger.Log(userId, $"Received reminder removed");
            }
            var syncResult = await focusCalendarSyncService.SyncAsync(userId);
            var lastLocation = await locationService.GetLastLocationAsync(userId);
            var res = await ManageFocus(userId, lastLocation);
            await locationService.RequestLocationAsync(userId, DateTimeOffset.Now, res);  //only request on changed or added?
        }

        public async Task PatchAsync(string userId)
        {
            await ManageFocus(userId, await locationService.GetLastLocationAsync(userId));
        }
    }
}
