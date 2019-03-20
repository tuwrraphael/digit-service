using Digit.Abstractions.Service;
using DigitService.Controllers;
using DigitService.Models;
using DigitService.Service;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task ClearGeofenceAsync(string userId)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleAsync();
            user.GeofenceFrom = null;
            user.GeofenceTo = null;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task<Location> GetLastLocationAsync(string userId)
        {
            var user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleOrDefaultAsync();
            return user?.StoredLocation?.MapToLocation();
        }

        public async Task<bool> IsGeofenceActiveAsync(string userId, GeofenceRequest when)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleAsync();
            return user.GeofenceFrom.HasValue && user.GeofenceTo.HasValue &&
                new DateTimeOffset(user.GeofenceFrom.Value, TimeSpan.Zero) <= when.Start &&
                new DateTimeOffset(user.GeofenceTo.Value, TimeSpan.Zero) >= when.End;
        }

        public async Task SetGeofenceRequestedAsync(string userId, GeofenceRequest request)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleAsync();
            user.GeofenceFrom = request.Start.UtcDateTime;
            user.GeofenceTo = request.End.UtcDateTime;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task UpdateLocationAsync(string userId, Location location)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleAsync();
            user.StoredLocation = user.StoredLocation ?? new StoredLocation();
            user.StoredLocation.MapFromLocation(location);
            await digitServiceContext.SaveChangesAsync();
        }
    }
}
