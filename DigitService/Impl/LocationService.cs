﻿using Digit.Abstractions.Service;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus;
using Digit.Focus.Model;
using Digit.Focus.Models;
using DigitService.Models;
using DigitService.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    public class LocationService : ILocationService
    {
        private readonly IPushSyncService pushSyncService;
        private readonly ILocationStore locationStore;
        private readonly IDigitLogger logger;

        public LocationService(IPushSyncService pushSyncService, ILocationStore locationStore, IDigitLogger logger)
        {
            this.pushSyncService = pushSyncService;
            this.locationStore = locationStore;
            this.logger = logger;
        }

        public async Task<Location> GetLastLocationAsync(string userId)
        {
            return await locationStore.GetLastLocationAsync(userId);
        }

        private bool DepartureIsPending(FocusItemWithExternalData v, DateTimeOffset now) => (v.IndicateTime - now) < FocusConstants.NoUpdateBeforeDepartureMargin && v.IndicateTime > now;

        private async Task<DateTimeOffset?> RequestLocationForDepartures(string userId, FocusManageResult manageResult, DateTimeOffset now, DateTimeOffset locationTimeStamp)
        {
            bool departureIsUpcoming(FocusItemWithExternalData v) => (v.IndicateTime - now) >= FocusConstants.NoUpdateBeforeDepartureMargin;
            DateTimeOffset halfTimeToDeparture(FocusItemWithExternalData v) => locationTimeStamp + ((v.IndicateTime - locationTimeStamp) / 2);
            var upcomingDeparturesLocationReqiurement = manageResult.ActiveItems
                .Where(v => null != v.Directions)
                .Where(departureIsUpcoming)
                .Select(v => new { Type = "upcoming", Time = halfTimeToDeparture(v), Departure = v });
            DateTimeOffset halfTimeToFirstStop(FocusItemWithExternalData v) => v.IndicateTime + ((v.Directions.Routes[v.DirectionsMetadata.PeferredRoute].Steps[0].DepartureTime - v.IndicateTime) / 2);
            var routeLocationRequirement = manageResult.ActiveItems
                .Where(v => null != v.Directions)
                .Where(v => DepartureIsPending(v, now))
                .Select(v => new { Type = "route", Time = halfTimeToFirstStop(v), Departure = v });

            var requirement = upcomingDeparturesLocationReqiurement.Union(routeLocationRequirement)
                .OrderBy(v => v.Time).FirstOrDefault();
            if (null != requirement)
            {
                await logger.Log(userId, $"Location update required for {requirement.Type} {requirement.Departure.CalendarEvent?.Subject} at {requirement.Time:s}");
                return requirement.Time;
            }
            return null;
        }

        private const int Radius = 40;

        private IEnumerable<GeofenceRequest> GetGeofences(FocusItemWithExternalData item)
        {
            var route = item.Directions.Routes[item.DirectionsMetadata.PeferredRoute];
            yield return new GeofenceRequest()
            {
                Id = $"start#{item.Id}",
                FocusItemId = item.Id,
                End = item.End,
                Start = route.DepatureTime.AddMinutes(-15),
                Lat = route.StartLocation.Lat,
                Lng = route.StartLocation.Lng,
                Exit = true,
                Radius = Radius
            };
            for (var i = 0; i < route.Steps.Length; i++)
            {
                var step = route.Steps[i];
                yield return new GeofenceRequest()
                {
                    Id = $"step{i}#{item.Id}",
                    FocusItemId = item.Id,
                    End = item.End,
                    Start = route.DepatureTime.AddMinutes(-15),
                    Lat = step.DepartureStop.Location.Lat,
                    Lng = step.DepartureStop.Location.Lat,
                    Exit = false,
                    Radius = Radius
                };
            }
            yield return new GeofenceRequest()
            {
                Id = $"end#{item.Id}",
                FocusItemId = item.Id,
                End = item.End,
                Start = route.DepatureTime.AddMinutes(-15),
                Lat = route.EndLocation.Lat,
                Lng = route.EndLocation.Lng,
                Exit = false,
                Radius = Radius
            };
        }

        private async Task<GeofenceRequest[]> RequestGeofencesForActiveNavigations(string userId, FocusManageResult manageResult,
            DateTimeOffset now)
        {
            var geofences = manageResult.ActiveItems.Where(v => null != v.DirectionsMetadata && null == v.DirectionsMetadata.Error)
                .Where(v => v.Directions.Routes[v.DirectionsMetadata.PeferredRoute].DepatureTime - now < FocusConstants.DeparturePending)
                .SelectMany(item => GetGeofences(item)).ToArray();
            await locationStore.SetGeofenceRequests(userId, geofences);
            return geofences;
        }

        private async Task CheckGeofenceTriggeredAsync(string userId, Location newLocation, DateTimeOffset now)
        {
            var gfs = await locationStore.GetNonExpiredGeofenceRequests(userId, now);
            foreach (var gf in gfs)
            {
                var distance = Geolocation.GeoCalculator.GetDistance(gf.Lat,
                        gf.Lng,
                        newLocation.Latitude,
                        newLocation.Longitude, 5, Geolocation.DistanceUnit.Meters);
                if (gf.Exit)
                {
                    if (distance >= gf.Radius)
                    {
                        await logger.Log(userId, $"Geofence {gf.Id}/Exit triggered");
                    }
                }
                else
                {
                    if (distance <= gf.Radius)
                    {
                        await logger.Log(userId, $"Geofence {gf.Id}/Enter triggered");
                    }
                }
            }
        }

        public async Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location, DateTimeOffset now, FocusManageResult focusManageResult)
        {
            await pushSyncService.SetDone(userId, new LocationPushSyncRequest(now));
            await CheckGeofenceTriggeredAsync(userId, location, now);
            await locationStore.UpdateLocationAsync(userId, location);
            //if (location.RequestSupport.HasValue && !location.RequestSupport.Value)
            //{
            //    return new LocationResponse();
            //}
            var response = new LocationResponse()
            {
                NextUpdateRequiredAt = await RequestLocationForDepartures(userId, focusManageResult, now, location.Timestamp),
                Geofences = await RequestGeofencesForActiveNavigations(userId, focusManageResult, now)
            };
            if (response.NextUpdateRequiredAt.HasValue)
            {
                await pushSyncService.SetRequestedExternal(userId, new LocationPushSyncRequest(response.NextUpdateRequiredAt.Value));
            }
            return response;
        }

        public async Task<LocationRequestResult> RequestLocationAsync(string userId, DateTimeOffset requestTime, FocusManageResult focusManageResult)
        {
            var storedLocation = await locationStore.GetLastLocationAsync(userId);
            bool requestLocation = false;
            string requestReason = null;
            if (null == storedLocation)
            {
                requestLocation = true;
                requestReason = "No Location stored";
            }
            else if (storedLocation.Timestamp < (requestTime - FocusConstants.LastLocationCacheTime))
            {
                requestLocation = true;
                requestReason = $"Last Location outdated {(requestTime - storedLocation.Timestamp).TotalMinutes:0}";
            }
            if (requestLocation)
            {
                var res = await pushSyncService.RequestSync(userId,
                    new LocationPushSyncRequest(requestTime.Add(FocusConstants.LocationRequestExpectedTime)),
                    requestTime);
                return new LocationRequestResult()
                {
                    LocationRequestSent = res.SyncRequested,
                    LocationRequestTime = res.SyncRequested ? requestTime : res.SyncPendingFor
                };
            }
            else
            {
                await logger.Log(userId, $"Not requesting location because cached location from {storedLocation.Timestamp:s} is used");
                return new LocationRequestResult()
                {
                    LocationRequestSent = false,
                    LocationRequestTime = null
                };
            }
        }
    }
}
