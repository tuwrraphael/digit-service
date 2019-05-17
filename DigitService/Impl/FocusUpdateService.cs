﻿using CalendarService.Client;
using CalendarService.Models;
using Digit.Abstractions.Service;
using Digit.Focus;
using Digit.Focus.Model;
using Digit.Focus.Models;
using Digit.Focus.Service;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IEnumerable<IFocusSubscriber> focusSubscribers;
        private readonly IFocusGeofenceService _focusGeofenceService;

        public FocusUpdateService(IFocusStore focusStore,
            ICalendarServiceClient calendarServiceClient,
            ITravelServiceClient travelServiceClient,
            IDigitLogger logger,
            IEnumerable<IFocusSubscriber> focusSubscribers,
            IFocusGeofenceService focusGeofenceService)
        {
            this.focusStore = focusStore;
            this.calendarServiceClient = calendarServiceClient;
            this.travelServiceClient = travelServiceClient;
            this.logger = logger;
            this.focusSubscribers = focusSubscribers;
            _focusGeofenceService = focusGeofenceService;
        }


        private async Task<string> ResolveAddress(string userId, Event evt)
        {
            string bySubject = $"#event:{evt.Subject}";
            if (await travelServiceClient.Users[userId].Locations[bySubject].Resolve())
            {
                return bySubject;
            }
            return evt.GetFormattedAddress();
        }

        private async Task<TransitDirections> GetFreshDirections(string userId, Event evt, FocusItem item, Location location)
        {
            string directionsKey = null;
            var address = await ResolveAddress(userId, evt);
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
                    const int preferredRoute = 0;
                    if (!requestWithNow)
                    {
                        directionsResult = await travelServiceClient.Users[userId].Directions.Transit.Get(start, address, evt.Start);
                        if (null != directionsResult.NotFound ||
                            directionsResult.TransitDirections.Routes[preferredRoute].DepatureTime < DateTimeOffset.Now)
                        {
                            requestWithNow = true;
                        }
                    }
                    if (requestWithNow)
                    {
                        directionsResult = await travelServiceClient.Users[userId].Directions.Transit.Get(start, address, null, DateTimeOffset.Now);
                    }
                    directionsKey = directionsResult?.CacheKey;
                    await focusStore.UpdateDirections(item.Id, directionsResult,
                        null != directionsResult.NotFound ? null : (int?)preferredRoute);
                    item.DirectionsMetadata = new DirectionsMetadata()
                    {
                        Error = directionsResult.NotFound?.Reason,
                        Key = directionsResult.CacheKey,
                        PeferredRoute = preferredRoute
                    };
                    return directionsResult.TransitDirections;
                }
                catch (TravelServiceException ex)
                {
                    await logger.Log(userId, $"Error while retrieving directions for {evt.Subject}: {ex.Message}", 3);
                }
            }
            // TODO maybe clear directions with focusStore.UpdateDirections
            return null;
        }

        private async Task<bool> RouteUpdateRequired(string userId, DirectionsResult res, int preferredRoute,
            Location location, DateTimeOffset now)
        {
            var traceMeasures = await travelServiceClient.Directions[res.CacheKey].Itineraries[preferredRoute]
                .Trace(new TraceLocation()
                {
                    Accuracy = new TraceLocationAccuracy()
                    {
                        Confidence = 0.68,
                        Radius = location.Accuracy
                    },
                    Coordinate = new Coordinate(location.Latitude, location.Longitude),
                    Timestamp = location.Timestamp
                });
            await logger.Log(userId, $"Traced {Math.Round(traceMeasures.ConfidenceOnRoute * 100)}% on route at accuracy of" +
                $"{location.Accuracy}, " +
                $" with delay of {traceMeasures.PositionOnRoute.Delay}");
            if (traceMeasures.ConfidenceOnRoute > 0.3)
            {
                if (traceMeasures.PositionOnRoute.Delay < FocusConstants.MaxAllowedDelay &&
                    (-FocusConstants.MaxAllowedEarly) < traceMeasures.PositionOnRoute.Delay)
                {
                    return false;
                }
            }
            return true;
        }

        private async Task<DirectionsUpdateResult> GetCachedDirectionsOrNew(string userId, Event evt, FocusItem item, Location location)
        {
            var directionsResult = await travelServiceClient.Directions[item.DirectionsMetadata.Key].GetAsync();
            if (null != directionsResult)
            {
                if (!await RouteUpdateRequired(userId,
                        directionsResult,
                        item.DirectionsMetadata.PeferredRoute,
                        location,
                        DateTimeOffset.Now))
                {
                    return new DirectionsUpdateResult()
                    {
                        Directions = directionsResult.TransitDirections,
                        IsNew = false
                    };
                }
            }
            return new DirectionsUpdateResult()
            {
                Directions = await GetFreshDirections(userId, evt, item, location),
                IsNew = true
            };
        }

        private class DirectionsUpdateResult
        {
            public TransitDirections Directions { get; set; }
            public bool IsNew { get; set; }
        }

        public async Task<FocusManageResult> Update(string userId, FocusUpdateRequest focusUpdateRequest)
        {
            await _focusGeofenceService.UpdateFocusItems(userId, focusUpdateRequest.Location);
            var res = new FocusManageResult();
            var activeItems = await focusStore.GetActiveAsync(userId);
            var updatedItemIds = new HashSet<string>(focusUpdateRequest.ItemSyncResult != null ?
                focusUpdateRequest.ItemSyncResult.ChangedItems.Select(v => v.Id) :
                new string[0]);
            foreach (var item in activeItems)
            {
                var evt = await calendarServiceClient.Users[userId].Feeds[item.CalendarEventFeedId].Events.Get(item.CalendarEventId);
                TransitDirections directions;
                if (null != focusUpdateRequest.ItemSyncResult && (
                    focusUpdateRequest.ItemSyncResult.AddedItems.Any(v => v.Id == item.Id) ||
                    focusUpdateRequest.ItemSyncResult.ChangedItems.Any(v => v.Id == item.Id))
                    || null == item.DirectionsMetadata?.Key)
                {
                    directions = await GetFreshDirections(userId, evt, item, focusUpdateRequest.Location);
                }
                else
                {
                    var directionsRes = await GetCachedDirectionsOrNew(userId, evt, item, focusUpdateRequest.Location);
                    directions = directionsRes.Directions;
                    if (directionsRes.IsNew)
                    {
                        updatedItemIds.Add(item.Id);
                    }
                }
                DateTimeOffset indicateTime;
                if (null == directions)
                {
                    await logger.Log(userId, $"No departure time found, using {FocusConstants.DefaultTravelTime.TotalMinutes:0} minutes for {evt.Subject}");
                    indicateTime = evt.Start - FocusConstants.DefaultTravelTime;
                }
                else
                {
                    indicateTime = directions.Routes[item.DirectionsMetadata.PeferredRoute].DepatureTime;
                }
                item.IndicateTime = indicateTime;
                await focusStore.UpdateIndicateTime(item.Id, indicateTime);
                res.ActiveItems.Add(new FocusItemWithExternalData()
                {
                    Start = item.Start,
                    IndicateTime = item.IndicateTime,
                    CalendarEvent = evt,
                    Directions = directions,
                    DirectionsMetadata = item.DirectionsMetadata,
                    End = item.End,
                    Id = item.Id
                });
            }
            await _focusGeofenceService.RefreshGeofencesForActiveNavigations(userId, res, DateTimeOffset.Now);
            var active = await focusStore.GetActiveItem(userId);
            var activeItemChanged = await focusStore.UpdateActiveItem(userId, active?.Id);
            if (activeItemChanged || (null != active && updatedItemIds.Contains(active.Id)))
            {
                if (null != active)
                {
                    await Task.WhenAll(focusSubscribers.Select(v => v.ActiveItemChanged(userId, res.ActiveItems.Where(d => d.Id == active.Id).Single())));
                }
                else
                {
                    await Task.WhenAll(focusSubscribers.Select(v => v.ActiveItemChanged(userId, null)));
                }
            }
            if (updatedItemIds.Count > 0)
            {
                await Task.WhenAll(focusSubscribers.Select(v => v.ActiveItemsChanged(userId, res)));
            }
            return res;
        }
    }
}
