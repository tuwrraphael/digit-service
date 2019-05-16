using Digit.Abstractions.Service;
using Digit.Focus.Model;
using DigitService.Models;
using DigitService.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl.EF
{

    public static class StoredLocationExtension
    {
        public static void MapFromLocation(this StoredLocation storedLocation, Location location)
        {
            storedLocation.Accuracy = location.Accuracy;
            storedLocation.Lat = location.Latitude;
            storedLocation.Lng = location.Longitude;
            storedLocation.Timestamp = location.Timestamp.UtcDateTime;
        }
        public static Location MapToLocation(this StoredLocation storedLocation)
        {
            return new Location()
            {
                Latitude = storedLocation.Lat,
                Longitude = storedLocation.Lng,
                Timestamp = new DateTimeOffset(storedLocation.Timestamp, TimeSpan.Zero),
                Accuracy = storedLocation.Accuracy,
            };
        }
    }

    public class LocationStore : ILocationStore
    {
        private readonly DigitServiceContext digitServiceContext;
        private readonly IUserRepository userRepository;
        private readonly IDigitLogger logger;

        public LocationStore(DigitServiceContext digitServiceContext, IUserRepository userRepository,
            IDigitLogger logger)
        {
            this.digitServiceContext = digitServiceContext;
            this.userRepository = userRepository;
            this.logger = logger;
        }

        public async Task<Location> GetLastLocationAsync(string userId)
        {
            var user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleOrDefaultAsync();
            return user?.StoredLocation?.MapToLocation();
        }

        public async Task<GeofenceRequest[]> SetGeofenceRequests(string userId, GeofenceRequest[] request)
        {
            List<GeofenceRequest> results = new List<GeofenceRequest>();
            foreach (var r in request)
            {
                var triggered = false;
                var focusItem = digitServiceContext.FocusItems.Where(v => v.UserId == userId && v.Id == r.FocusItemId)
                    .Include(v => v.Geofences)
                    .SingleOrDefault();
                if (null == focusItem)
                {
                    continue;
                }
                var gf = focusItem.Geofences.Where(g => g.Id == r.Id).SingleOrDefault();
                if (null != gf)
                {
                    gf.Triggered = (r.Exit == gf.Exit &&
                        r.Lat == gf.Lat &&
                        r.Lng == gf.Lng &&
                        r.Radius == gf.Radius
                        && r.Start.UtcDateTime == gf.Start) ? gf.Triggered : false;

                    if (gf.Triggered == true)
                    {
                        triggered = true;
                    }
                    else
                    {
                        gf.Lat = r.Lat;
                        gf.Lng = r.Lng;
                        gf.Radius = r.Radius;
                        gf.Start = r.Start.UtcDateTime;
                        gf.End = r.End.UtcDateTime;
                        gf.Exit = r.Exit;
                    }
                }
                else
                {
                    focusItem.Geofences.Add(new StoredGeoFence()
                    {
                        End = r.End.UtcDateTime,
                        Exit = r.Exit,
                        FocusItemId = r.FocusItemId,
                        Id = r.Id,
                        Lat = r.Lat,
                        Lng = r.Lng,
                        Radius = r.Radius,
                        Start = r.Start.UtcDateTime,
                        Triggered = false
                    });
                }
                if (!triggered)
                {
                    results.Add(r);
                }
            }
            await digitServiceContext.SaveChangesAsync();
            return results.ToArray();
        }


        public async Task UpdateLocationAsync(string userId, Location location)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleAsync();
            user.StoredLocation = user.StoredLocation ?? new StoredLocation();
            user.StoredLocation.MapFromLocation(location);
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task<GeofenceRequest[]> GetNonExpiredGeofenceRequests(string userId, DateTimeOffset now)
        {
            return await digitServiceContext.FocusItems
                .Include(v => v.Geofences)
                .Where(v => v.UserId == userId)
                .SelectMany(v => v.Geofences)
                .Where(v => v.Start <= now.UtcDateTime && now.UtcDateTime <= v.End)
                .Where(v => !v.Triggered)
                .Select(v => new GeofenceRequest()
                {
                    End = new DateTimeOffset(v.End, TimeSpan.Zero),
                    Start = new DateTimeOffset(v.Start, TimeSpan.Zero),
                    Exit = v.Exit,
                    FocusItemId = v.FocusItemId,
                    Id = v.Id,
                    Lat = v.Lat,
                    Lng = v.Lng,
                    Radius = v.Radius
                }).ToArrayAsync();
        }
    }
}
