using DigitService.Models;
using System;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface ILocationStore
    {
        Task StoreLocationAsync(string userId, Location location);
        Task<Location> GetLastLocationAsync(string userId);
        Task<DateTime?> GetLocationRequestTimeAsync(string userId);
        Task SetLocationRequestedForAsync(string userId, DateTime dateTime);
    }
}
