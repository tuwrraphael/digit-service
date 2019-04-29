using ButlerClient;
using CalendarService.Client;
using CalendarService.Models;
using Digit.Abstractions.Service;
using Digit.Focus;
using Digit.Focus.Models;
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

namespace DigitService.Impl
{
    public class FocusUpdateService : IFocusUpdateService
    {
        private readonly IFocusStore focusStore;
        private readonly ICalendarServiceClient calendarServiceClient;
        private readonly ITravelServiceClient travelServiceClient;
        private readonly IDigitLogger logger;
        private readonly IButler butler;
        private readonly IDigitPushServiceClient digitPushServiceClient;
        private readonly IEnumerable<IFocusSubscriber> focusSubscribers;
        private readonly DigitServiceOptions options;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _notifySempahores = new ConcurrentDictionary<string, SemaphoreSlim>();

        public FocusUpdateService(IFocusStore focusStore,
            ICalendarServiceClient calendarServiceClient,
            ITravelServiceClient travelServiceClient,
            IDigitLogger logger,
            IButler butler,
            IOptions<DigitServiceOptions> optionsAccessor,
            IDigitPushServiceClient digitPushServiceClient,
            IEnumerable<IFocusSubscriber> focusSubscribers)
        {
            this.focusStore = focusStore;
            this.calendarServiceClient = calendarServiceClient;
            this.travelServiceClient = travelServiceClient;
            this.logger = logger;
            this.butler = butler;
            this.digitPushServiceClient = digitPushServiceClient;
            this.focusSubscribers = focusSubscribers;
            options = optionsAccessor.Value;
        }



        private async Task<Route> GetNewRoute(string userId, Event evt, FocusItem item, Location location)
        {
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
                        directionsResult = await travelServiceClient.Users[userId].Directions.Transit.Get(start, address, evt.Start);
                        route = DirectionUtils.SelectRoute(directionsResult);
                        if (null != route && route.DepatureTime < DateTimeOffset.Now)
                        {
                            requestWithNow = true;
                        }
                    }
                    if (requestWithNow)
                    {
                        directionsResult = await travelServiceClient.Users[userId].Directions.Transit.Get(start, address, null, DateTimeOffset.Now);
                        route = DirectionUtils.SelectRoute(directionsResult);
                    }
                    directionsKey = directionsResult?.CacheKey;
                    if (null != route)
                    {
                        await focusStore.UpdateDirections(item.Id, directionsKey);
                    }
                }
                catch (TravelServiceException ex)
                {
                    await logger.Log(userId, $"Error while retrieving directions for {evt.Subject}: {ex.Message}", 3);
                }
            }
            return route;
        }

        private bool RouteUpdateRequired(Route route, Location location, DateTimeOffset now)
        {
            if (route.DepatureTime > now)
            {
                return false;
            }
            Step relevantStep = route.Steps.Where(s => s.DepartureTime <= now && now < s.ArrivalTime)
                .FirstOrDefault();
            if (null == relevantStep)
            {
                return true;
            }
            var distanceToDeparture = Geolocation.GeoCalculator.GetDistance(location.Latitude,
                        location.Longitude,
                        relevantStep.DepartureStop.Location.Lat,
                        relevantStep.DepartureStop.Location.Lng, 2, Geolocation.DistanceUnit.Meters);
            var distanceToArrival = Geolocation.GeoCalculator.GetDistance(location.Latitude,
                location.Longitude,
                relevantStep.ArrivalStop.Location.Lat,
                relevantStep.ArrivalStop.Location.Lng, 2, Geolocation.DistanceUnit.Meters);
            var distanceDepartureArrival =
                Geolocation.GeoCalculator.GetDistance(relevantStep.DepartureStop.Location.Lat,
                        relevantStep.DepartureStop.Location.Lng,
                relevantStep.ArrivalStop.Location.Lat,
                relevantStep.ArrivalStop.Location.Lng, 2, Geolocation.DistanceUnit.Meters);

            var timePercentage = (now - relevantStep.DepartureTime) / (relevantStep.ArrivalTime - relevantStep.DepartureTime);
            var wayPercentage = distanceToDeparture / distanceDepartureArrival;
            var inellipse = (distanceToArrival + distanceToDeparture) < 1.4 * distanceDepartureArrival;
            var similartravel = Math.Abs(timePercentage - wayPercentage) < 0.2;
            return similartravel && inellipse;
        }

        private async Task<RouteUpdateResult> GetUpdatedOrNew(string userId, Event evt, FocusItem item, Location location)
        {
            if (null != item.DirectionsKey)
            {
                var directionsResult = await travelServiceClient.Directions[item.DirectionsKey].GetAsync();
                if (null != directionsResult)
                {
                    var route = DirectionUtils.SelectRoute(directionsResult);
                    if (null != route && !RouteUpdateRequired(route, location, DateTimeOffset.Now))
                    {
                        return new RouteUpdateResult()
                        {
                            Route = route,
                            IsNew = false
                        };
                    }
                }
            }
            return new RouteUpdateResult()
            {
                Route = await GetNewRoute(userId, evt, item, location),
                IsNew = true
            };
        }

        private class RouteUpdateResult
        {
            public Route Route { get; set; }
            public bool IsNew { get; set; }
        }

        public async Task<FocusManageResult> Update(string userId, FocusUpdateRequest focusUpdateRequest)
        {
            var res = new FocusManageResult();
            var activeItems = await focusStore.GetActiveAsync(userId);
            var updatedItemIds = new HashSet<string>(focusUpdateRequest.ItemSyncResult != null ?
                focusUpdateRequest.ItemSyncResult.ChangedItems.Select(v => v.Id) :
                new string[0]);
            foreach (var item in activeItems)
            {
                var evt = await calendarServiceClient.Users[userId].Feeds[item.CalendarEventFeedId].Events.Get(item.CalendarEventId);
                Route route;
                if (null != focusUpdateRequest.ItemSyncResult && (
                    focusUpdateRequest.ItemSyncResult.AddedItems.Any(v => v.Id == item.Id) ||
                    focusUpdateRequest.ItemSyncResult.ChangedItems.Any(v => v.Id == item.Id)))
                {
                    route = await GetNewRoute(userId, evt, item, focusUpdateRequest.Location);
                }
                else
                {
                    var routeRes = await GetUpdatedOrNew(userId, evt, item, focusUpdateRequest.Location);
                    route = routeRes.Route;
                    if (routeRes.IsNew)
                    {
                        updatedItemIds.Add(item.Id);
                    }
                }
                DateTimeOffset departureTime;
                if (null == route)
                {
                    await logger.Log(userId, $"No departure time found, using {FocusConstants.DefaultTravelTime.TotalMinutes:0} minutes for {evt.Subject}");
                    departureTime = evt.Start - FocusConstants.DefaultTravelTime;
                }
                else
                {
                    departureTime = route.DepatureTime;
                    res.Departures.Add(new FocusDeparture()
                    {
                        DepartureTime = departureTime,
                        Route = route,
                        Event = evt
                    });
                }
                item.IndicateTime = departureTime;
                await focusStore.UpdateIndicateTime(item.Id, departureTime);
                await NotifyOrInstall(userId, item, evt, departureTime);
            }
            var active = await focusStore.GetActiveItem(userId);
            var activeItemChanged = await focusStore.UpdateActiveItem(userId, active?.Id);
            if (activeItemChanged || (null != active && updatedItemIds.Contains(active.Id)))
            {
                await Task.WhenAll(focusSubscribers.Select(v => v.ActiveItemChanged(userId, active)));
            }
            if (updatedItemIds.Count > 0)
            {
                await Task.WhenAll(focusSubscribers.Select(v => v.ActiveItemsChanged(userId, activeItems)));
            }
            return res;
        }

        private async Task NotifyOrInstall(string userId, FocusItem item, Event evt, DateTimeOffset departureTime)
        {
            var timeToDeparture = departureTime - DateTimeOffset.Now;
            if (timeToDeparture < FocusConstants.NotifyTime.Add(FocusConstants.ButlerInaccuracy))
            {
                var notifySemaphore = _notifySempahores.GetOrAdd(userId, s => new SemaphoreSlim(1));
                await notifySemaphore.WaitAsync();
                try
                {
                    if (!await focusStore.FocusItemNotifiedAsync(item.Id))
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
                        await focusStore.SetFocusItemNotifiedAsync(item.Id); // always set notified for now to prevent massive notification spam
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
                    When = departureTime.Add(-FocusConstants.NotifyTime).UtcDateTime,
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
