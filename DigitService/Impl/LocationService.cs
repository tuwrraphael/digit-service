using Digit.Abstractions.Service;
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

namespace DigitService.Impl
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

        private class UpdateRequirement
        {
            public DateTimeOffset Time { get; set; }
            public string Reason { get; set; }
            public FocusItemWithExternalData FocusItem { get; set; }
        }


        private IEnumerable<UpdateRequirement> UpdateTimes(IEnumerable<FocusItemWithExternalData> candidates,
            DateTimeOffset now, DateTimeOffset locationTimeStamp)
        {
            foreach (var item in candidates)
            {
                var preferredRoute = item.Directions.Routes[item.DirectionsMetadata.PeferredRoute];
                if (preferredRoute.DepatureTime - now > FocusConstants.DeparturePending)
                {
                    yield return new UpdateRequirement()
                    {
                        Time = locationTimeStamp + ((preferredRoute.DepatureTime - locationTimeStamp) / 2.0),
                        Reason = "departure",
                        FocusItem = item
                    };
                }
                else
                {
                    if (preferredRoute.Steps.Length > 0 && preferredRoute.Steps[0].DepartureTime > now)
                    {
                        yield return new UpdateRequirement()
                        {
                            Time = preferredRoute.DepatureTime + ((preferredRoute.Steps[0].DepartureTime - preferredRoute.DepatureTime) / 2.0),
                            Reason = "half time to first step",
                            FocusItem = item
                        };
                    }
                    else
                    {
                        var nextStepDeparture = preferredRoute.Steps.Where(step => step.DepartureTime > now).FirstOrDefault();
                        if (null != nextStepDeparture)
                        {
                            yield return new UpdateRequirement()
                            {
                                Time = nextStepDeparture.DepartureTime,
                                Reason = "step",
                                FocusItem = item
                            };
                        }
                        else
                        {
                            yield return new UpdateRequirement()
                            {
                                Time = preferredRoute.ArrivalTime,
                                Reason = "arrival",
                                FocusItem = item
                            };
                        }
                    }
                }
            }
        }

        private async Task<DateTimeOffset?> RequestLocationForItems(string userId, FocusManageResult manageResult, DateTimeOffset now, DateTimeOffset locationTimeStamp)
        {
            var candidates = manageResult.ActiveItems.Where(
                v => null != v.DirectionsMetadata
                && null != v.Directions
                && v.DirectionsMetadata.TravelStatus != TravelStatus.Finished);
            var requirement = UpdateTimes(candidates, now, locationTimeStamp).OrderBy(v => v.Time).FirstOrDefault();
            if (null != requirement)
            {
                await logger.LogForFocusItem(userId, requirement.FocusItem.Id, $"Location update for {requirement.Reason} {requirement.FocusItem.CalendarEvent?.Subject} at {requirement.Time:t}");
                if (requirement.Time < now + FocusConstants.ShortestLocationUpdateTime)
                {
                    return now + FocusConstants.ShortestLocationUpdateTime;
                }
                return requirement.Time;
            }
            return null;
        }

        public async Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location, DateTimeOffset now, FocusManageResult focusManageResult)
        {
            await pushSyncService.SetLocationRequestDone(userId);
            await locationStore.UpdateLocationAsync(userId, location);
            //if (location.RequestSupport.HasValue && !location.RequestSupport.Value)
            //{
            //    return new LocationResponse();
            //}
            var response = new LocationResponse()
            {
                NextUpdateRequiredAt = await RequestLocationForItems(userId, focusManageResult, now, location.Timestamp),
                Geofences = await locationStore.GetActiveGeofenceRequests(userId, now)
            };
            if (response.NextUpdateRequiredAt.HasValue)
            {
                await pushSyncService.SetLocationRequestedExternal(userId, response.NextUpdateRequiredAt.Value);
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
                var res = await pushSyncService.RequestLocationSync(userId, requestTime, requestTime.Add(FocusConstants.LocationRequestExpectedTime));
                return new LocationRequestResult()
                {
                    LocationRequestSent = res.SyncRequested,
                    LocationRequestTime = res.SyncRequested ? requestTime : res.SyncPendingFor
                };
            }
            else
            {
                await logger.LogForUser(userId, $"Not requesting location because cached location from {storedLocation.Timestamp:s} is used");
                return new LocationRequestResult()
                {
                    LocationRequestSent = false,
                    LocationRequestTime = null
                };
            }
        }
    }
}
