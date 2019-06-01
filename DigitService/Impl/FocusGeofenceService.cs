using Digit.Abstractions.Service;
using Digit.Focus;
using Digit.Focus.Model;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitService.Models;
using DigitService.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class FocusGeofenceService : IFocusGeofenceService
    {
        public FocusGeofenceService(IDigitLogger logger,
            ILocationStore locationStore, IFocusStore focusStore)
        {
            _logger = logger;
            _locationStore = locationStore;
            _focusStore = focusStore;
        }
        private readonly IDigitLogger _logger;
        private readonly ILocationStore _locationStore;
        private readonly IFocusStore _focusStore;

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
                Radius = 50
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
                    Lng = step.DepartureStop.Location.Lng,
                    Exit = false,
                    Radius = 150
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
                Radius = 100
            };
        }

        public async Task<GeofenceRequest[]> GetNewGeofencesForActiveNavigations(string userId, FocusManageResult manageResult,
            DateTimeOffset now)
        {
            var geofences = manageResult.ActiveItems.Where(v => null != v.DirectionsMetadata && null == v.DirectionsMetadata.Error)
                .Where(v => v.Directions.Routes[v.DirectionsMetadata.PeferredRoute].DepatureTime - now < FocusConstants.DeparturePending)
                .SelectMany(item => GetGeofences(item));
            return (await Task.WhenAll(geofences.Select(async gfr => new { gfr, exists = await _locationStore.Exists(userId, gfr) })))
                .Where(v => !v.exists).Select(v => v.gfr).ToArray();
        }

        public async Task RefreshGeofencesForActiveNavigations(string userId, FocusManageResult manageResult,
            DateTimeOffset now)
        {
            var freshFences = await GetNewGeofencesForActiveNavigations(userId, manageResult, now);
            await _locationStore.AddGeofenceRequests(userId, freshFences);
        }

        public async Task<GeofenceRequest[]> GetTriggered(string userId, Location newLocation)
        {
            var gfs = (await _locationStore.GetActiveGeofenceRequests(userId, newLocation.Timestamp))
                .Select(async (gf) =>
               {
                   var triggered = false;
                   var distance = Geolocation.GeoCalculator.GetDistance(gf.Lat,
                           gf.Lng,
                           newLocation.Latitude,
                           newLocation.Longitude, 5, Geolocation.DistanceUnit.Meters);
                   if (gf.Exit)
                   {
                       if (distance >= gf.Radius)
                       {
                           await _logger.LogForFocusItem(userId, gf.FocusItemId, $"Geofence {gf.Id}/Exit triggered");
                           triggered = true;
                       }
                   }
                   else
                   {
                       if (distance <= gf.Radius)
                       {
                           await _logger.LogForFocusItem(userId, gf.FocusItemId, $"Geofence {gf.Id}/Enter triggered");
                           triggered = true;
                       }
                   }
                   return new { gf, triggered };
               });
            return (await Task.WhenAll(gfs)).Where(v => v.triggered).Select(v => v.gf).ToArray();
        }

        public async Task UpdateFocusItems(string userId, Location newLocation)
        {
            var triggered = await GetTriggered(userId, newLocation);
            foreach (var t in triggered)
            {
                if (t.Id.StartsWith("start"))
                {
                    await _focusStore.SetTravelStatus(userId, t.FocusItemId, TravelStatus.OnJourney);
                }
                else if (t.Id.StartsWith("end"))
                {
                    await _focusStore.SetTravelStatus(userId, t.FocusItemId, TravelStatus.Finished);
                }
                await _locationStore.SetTriggered(userId, triggered);
            }
        }
    }
}
