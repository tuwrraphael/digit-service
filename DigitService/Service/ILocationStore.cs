using DigitService.Models;
using System;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface ILocationStore
    {
        Task UpdateLocationAsync(string userId, Location location);
        Task<Location> GetLastLocationAsync(string userId);
        Task<DateTimeOffset?> GetLocationRequestTimeAsync(string userId);
        Task SetLocationRequestedForAsync(string userId, DateTimeOffset dateTime);
        Task<bool> IsGeofenceActiveAsync(string userId, GeofenceRequest when);
        Task SetGeofenceRequestedAsync(string userId, GeofenceRequest request);
        Task ClearGeofenceAsync(string userId);
    }
}
