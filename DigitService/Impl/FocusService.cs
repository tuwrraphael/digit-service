using ButlerClient;
using CalendarService.Client;
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
        private readonly ILocationStore locationStore;
        private readonly IDigitLogger logger;
        private readonly ICalendarServiceClient calendarServiceClient;
        private readonly ITravelServiceClient travelServiceClient;
        private readonly IButler butler;
        private readonly IDigitPushServiceClient digitPushServiceClient;
        private readonly DigitServiceOptions options;
        private readonly IFocusStore focusStore;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _notifySempahores = new ConcurrentDictionary<string, SemaphoreSlim>();

        private static readonly TimeSpan NotifyTime = new TimeSpan(0, 6, 0);
        private static readonly TimeSpan ButlerInaccuracy = new TimeSpan(0, 1, 0);
        private static readonly TimeSpan LastLocationCacheTime = new TimeSpan(0, 5, 0);
        private static readonly TimeSpan LocationRequestInvalidationTime = new TimeSpan(0, 20, 0);
        private static readonly TimeSpan NoUpdateBeforeDepartureMargin = new TimeSpan(0, 10, 0);
        public static readonly TimeSpan FocusScanTime = new TimeSpan(2, 0, 0);
        private static readonly TimeSpan CalendarServiceInacurracy = new TimeSpan(0, 10, 0);
        private static readonly TimeSpan DefaultTravelTime = new TimeSpan(0, 45, 0);

        public FocusService(ILocationStore locationStore, IDigitLogger logger,
            ICalendarServiceClient calendarServiceClient, ITravelServiceClient travelServiceClient, IButler butler,
            IDigitPushServiceClient digitPushServiceClient,
            IFocusStore focusStore, IOptions<DigitServiceOptions> optionsAccessor)
        {
            this.locationStore = locationStore;
            this.logger = logger;
            this.calendarServiceClient = calendarServiceClient;
            this.travelServiceClient = travelServiceClient;
            this.butler = butler;
            this.digitPushServiceClient = digitPushServiceClient;
            options = optionsAccessor.Value;
            this.focusStore = focusStore;
        }


        public async Task NotifyCallbackAsync(NotifyUserRequest request)
        {
            await ManageFocus(request.UserId, await locationStore.GetLastLocationAsync(request.UserId));
        }

        private class ManageResult
        {
            public List<DateTimeOffset> DepartureTimes { get; set; } = new List<DateTimeOffset>();
        }

        private DateTimeOffset? GetDepartureTime(DirectionsResult directionsResult)
        {
            if (null != directionsResult?.TransitDirections && directionsResult.TransitDirections.Routes.Any())
            {
                if (directionsResult.TransitDirections.Routes.Where(v => v.DepatureTime.HasValue).Any())
                {
                    var route = directionsResult.TransitDirections.Routes.Where(v => v.DepatureTime.HasValue).First();
                    return route.DepatureTime.Value;
                }
            }
            return null;
        }


        private async Task<ManageResult> ManageFocus(string userId, Location location)
        {
            var res = new ManageResult();
            var events = await calendarServiceClient.Users[userId].Events.Get(DateTimeOffset.Now, DateTimeOffset.Now.Add(FocusScanTime + CalendarServiceInacurracy)) ?? new CalendarService.Models.Event[0];
            var notAllDay = events.Where(v => !v.IsAllDay).ToArray();
            string directionsKey = null;
            foreach (var evt in notAllDay)
            {
                DateTimeOffset? departureTime = null;
                var focusItem = await focusStore.GetForCalendarEventAsync(userId, evt);
                if (null == focusItem)
                {
                    focusItem = await focusStore.StoreCalendarEventAsync(userId, evt);
                }
                var address = evt.Location?.Address != null ?
                            $"{evt.Location.Address.Street}, {evt.Location.Address.PostalCode} {evt.Location.Address.City} {evt.Location.Address.CountryOrRegion}" : evt.Location?.Text;
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
                            departureTime = GetDepartureTime(directionsResult);
                            if (departureTime.HasValue && departureTime.Value < DateTimeOffset.Now)
                            {
                                requestWithNow = true;
                            }
                        }
                        if (requestWithNow)
                        {
                            directionsResult = await travelServiceClient.Directions.Transit.Get(start, address, null, DateTimeOffset.Now);
                            departureTime = GetDepartureTime(directionsResult);
                        }
                        directionsKey = directionsResult?.CacheKey;
                    }
                    catch (TravelServiceException ex)
                    {
                        await logger.Log(userId, $"Error while retrieving directions for {evt.Subject}: {ex.Message}", 3);
                    }
                }
                if (null == departureTime)
                {
                    await logger.Log(userId, $"No departure time found, using {DefaultTravelTime.TotalMinutes:0} minutes for {evt.Subject}");
                    departureTime = evt.Start - DefaultTravelTime;
                }
                else
                {
                    res.DepartureTimes.Add(departureTime.Value);
                }
                await focusStore.UpdateWithDirections(focusItem.Id, departureTime.Value, directionsKey);
                if (departureTime.Value - DateTimeOffset.Now < NotifyTime.Add(ButlerInaccuracy))
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
                                await focusStore.SetFocusItemNotifiedAsync(focusItem.Id);
                            }
                            catch (Exception e)
                            {
                                await logger.Log(userId, $"Could notify user ({e.Message}).", 3);
                            }
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
                        When = departureTime.Value.Add(-NotifyTime).UtcDateTime,
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
            await logger.Log(userId, $"Received Location {location.Longitude:0.00}/{location.Latitude:0.00}");
            await locationStore.StoreLocationAsync(userId, location);
            var manageResult = await ManageFocus(userId, location);

            var response = new LocationResponse()
            {
                NextUpdateRequiredAt = manageResult.DepartureTimes
                .Select(v => v - DateTimeOffset.Now)
                .Where(v => v >= NoUpdateBeforeDepartureMargin) // no location update in the last 10 minutes
                .Select(v => DateTimeOffset.Now + (v / 2))
                .OrderBy(v => v)
                .Select(v => (DateTime?)v.UtcDateTime)
                .FirstOrDefault()
            };
            if (response.NextUpdateRequiredAt.HasValue)
            {
                await locationStore.SetLocationRequestedForAsync(userId, response.NextUpdateRequiredAt.Value);
                await logger.Log(userId, $"A location update is required at {response.NextUpdateRequiredAt.Value:s}");
            }
            return response;
        }



        public async Task ReminderDeliveryAsync(string userId, ReminderDelivery reminderDelivery)
        {
            await logger.Log(userId, $"Received reminder for {reminderDelivery.Event.Subject}");
            if (null != await focusStore.GetForCalendarEventAsync(userId, reminderDelivery.Event))
            {
                await focusStore.UpdateCalendarEventAsync(userId, reminderDelivery.Event);
            }
            var storedLocation = await locationStore.GetLastLocationAsync(userId);
            var locationRequestTime = await locationStore.GetLocationRequestTimeAsync(userId);
            bool requestLocation = false;
            string requestReason = null;
            if (null == storedLocation)
            {
                requestLocation = true;
                requestReason = "No Location stored";
            }
            else if (storedLocation.Timestamp < (DateTime.Now - LastLocationCacheTime))
            {
                requestLocation = true;
                requestReason = $"Last Location outdated {(DateTime.Now - storedLocation.Timestamp).TotalMinutes:0}";
            }
            if (requestLocation)
            {
                bool lastRequestPending =
                    null != locationRequestTime && null != storedLocation &&
                    locationRequestTime > storedLocation.Timestamp && (DateTime.Now - locationRequestTime) < LocationRequestInvalidationTime;
                if (lastRequestPending)
                {
                    await logger.Log(userId, $"Not requesting location because last request is still pending ({locationRequestTime:s})");
                    // TODO schedule resend if request was not fulfilled via butler
                }
                else
                {
                    try
                    {
                        await digitPushServiceClient.Push[userId].Create(new DigitPushService.Models.PushRequest()
                        {
                            ChannelOptions = new Dictionary<string, string>()
                    {
                        { "digitLocationRequest", null}
                    },
                            Payload = JsonConvert.SerializeObject(new PushPayload() { Action = PushActions.SendLocation })
                        });
                        await locationStore.SetLocationRequestedForAsync(userId, DateTime.Now);
                        await logger.Log(userId, $"Sent push location request: {requestReason}");
                    }
                    catch (Exception e)
                    {
                        await logger.Log(userId, $"Failed to send push location request: {requestReason}; Error: {e.Message}");
                    }
                }
            }
            else
            {
                await logger.Log(userId, $"Not requesting location because cached location from {storedLocation.Timestamp:s} is used");
            }
            await ManageFocus(userId, storedLocation);
        }

        public async Task PatchAsync(string userId)
        {
            await ManageFocus(userId, await locationStore.GetLastLocationAsync(userId));
        }
    }
}
