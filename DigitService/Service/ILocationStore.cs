using Digit.Focus.Model;
using DigitService.Models;
using System;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface ILocationStore
    {
        Task UpdateLocationAsync(string userId, Location location);
        Task<Location> GetLastLocationAsync(string userId);
        Task<GeofenceRequest[]> GetActiveGeofenceRequests(string userId, DateTimeOffset now);
        Task AddGeofenceRequests(string userId, GeofenceRequest[] request);
        Task SetTriggered(string userId, GeofenceRequest[] request);
    }
}
