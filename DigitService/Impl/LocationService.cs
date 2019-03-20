using Digit.Abstractions.Service;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using DigitService.Models;
using DigitService.Service;
using System;
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

        private bool DepartureIsPending(FocusDeparture v, DateTimeOffset now) => (v.DepartureTime - now) < FocusConstants.NoUpdateBeforeDepartureMargin && v.DepartureTime > now;

        private async Task<DateTimeOffset?> RequestLocationForDepartures(string userId, FocusManageResult manageResult, DateTimeOffset now, DateTimeOffset locationTimeStamp)
        {
            bool departureIsUpcoming(FocusDeparture v) => (v.DepartureTime - now) >= FocusConstants.NoUpdateBeforeDepartureMargin;
            DateTimeOffset halfTimeToDeparture(FocusDeparture v) => locationTimeStamp + ((v.DepartureTime - locationTimeStamp) / 2);
            var upcomingDeparturesLocationReqiurement = manageResult.Departures
                .Where(departureIsUpcoming)
                .Select(v => new { Type = "upcoming", Time = halfTimeToDeparture(v), Departure = v });
            DateTimeOffset halfTimeToFirstStop(FocusDeparture v) => v.DepartureTime + ((v.Route.Steps[0].DepartureTime - v.DepartureTime) / 2);
            var routeLocationRequirement = manageResult.Departures
                .Where(v => DepartureIsPending(v, now))
                .Where(v => null != v.Route)
                .Select(v => new { Type = "route", Time = halfTimeToFirstStop(v), Departure = v });

            var requirement = upcomingDeparturesLocationReqiurement.Union(routeLocationRequirement)
                .OrderBy(v => v.Time).FirstOrDefault();
            if (null != requirement)
            {
                await logger.Log(userId, $"Location update required for {requirement.Type} {requirement.Departure.Event?.Subject} at {requirement.Time:s}");
                return requirement.Time;
            }
            return null;
        }

        private async Task<GeofenceRequest> RequestGeofenceForPendingDeparturesAsync(string userId, FocusManageResult manageResult,
            DateTimeOffset now)
        {
            var pendingDepartures = manageResult.Departures
                .Where(v => DepartureIsPending(v, now));
            if (pendingDepartures.Any())
            {
                var request = new GeofenceRequest()
                {
                    Start = now,
                    End = pendingDepartures.Select(v => v.Event.End).OrderByDescending(v => v).First()
                };
                if (await locationStore.IsGeofenceActiveAsync(userId, request))
                {
                    await logger.Log(userId, $"No Geofence requested because active");
                }
                else
                {
                    await logger.Log(userId, $"Requested Geofence {request.Start:s} to  {request.End:s}");
                    await locationStore.SetGeofenceRequestedAsync(userId, request);
                    return request;
                }
            }
            return null;
        }

        private async Task CheckGeofenceTriggeredAsync(string userId, Location newLocation, DateTimeOffset now)
        {
            if (await locationStore.IsGeofenceActiveAsync(userId, new GeofenceRequest()
            {
                End = now,
                Start = now
            }))
            {
                var oldLocation = await locationStore.GetLastLocationAsync(userId);
                if (null != oldLocation)
                {
                    var distance = Geolocation.GeoCalculator.GetDistance(oldLocation.Latitude,
                        oldLocation.Longitude,
                        newLocation.Latitude,
                        newLocation.Longitude, 2, Geolocation.DistanceUnit.Meters);
                    if (distance > (FocusConstants.GeofenceRadius * FocusConstants.GeofenceThreshold))
                    {
                        await logger.Log(userId, $"Set Geofence triggered");
                        await locationStore.ClearGeofenceAsync(userId);
                    }
                }
            }
        }

        public async Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location, DateTimeOffset now, FocusManageResult focusManageResult)
        {
            await CheckGeofenceTriggeredAsync(userId, location, now);
            await locationStore.UpdateLocationAsync(userId, location);
            var response = new LocationResponse()
            {
                NextUpdateRequiredAt = await RequestLocationForDepartures(userId, focusManageResult, now, location.Timestamp),
                RequestGeofence = await RequestGeofenceForPendingDeparturesAsync(userId, focusManageResult, now)
            };
            if (response.NextUpdateRequiredAt.HasValue)
            {
                await pushSyncService.SetRequestedExternal(userId, new LegacyLocationPushSyncRequest(response.NextUpdateRequiredAt.Value));
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
                    new LegacyLocationPushSyncRequest(requestTime.Add(FocusConstants.LocationRequestExpectedTime)),
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
