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
            storedLocation.Timestamp = location.Timestamp;
        }
        public static Location MapToLocation(this StoredLocation storedLocation)
        {
            return new Location()
            {
                Latitude = storedLocation.Lat,
                Longitude = storedLocation.Lng,
                Timestamp = storedLocation.Timestamp,
                Accuracy = storedLocation.Accuracy,
            };
        }
    }

    public class LocationStore : ILocationStore
    {
        private readonly DigitServiceContext digitServiceContext;
        private readonly IUserRepository userRepository;

        public LocationStore(DigitServiceContext digitServiceContext, IUserRepository userRepository)
        {
            this.digitServiceContext = digitServiceContext;
            this.userRepository = userRepository;
        }

        public async Task<Location> GetLastLocationAsync(string userId)
        {
            var user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleOrDefaultAsync();
            return user?.StoredLocation?.MapToLocation();
        }

        public async Task<DateTime?> GetLocationRequestTimeAsync(string userId)
        {
            var user = await userRepository.GetAsync(userId);
            if (null == user)
            {
                return null;
            }
            return user.LocationRequestTime;
        }

        public async Task SetLocationRequestedForAsync(string userId, DateTime dateTime)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            user.LocationRequestTime = dateTime;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task StoreLocationAsync(string userId, Location location)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            user = await digitServiceContext.Users.Include(v => v.StoredLocation).Where(v => v.Id == userId).SingleAsync();
            user.StoredLocation = user.StoredLocation ?? new StoredLocation();
            user.StoredLocation.MapFromLocation(location);
            await digitServiceContext.SaveChangesAsync();
        }
    }
}
