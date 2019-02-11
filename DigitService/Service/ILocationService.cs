using DigitService.Models;
using System;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface ILocationService
    {
        Task<LocationRequestResult> RequestLocationAsync(string userId, DateTimeOffset requestTime, FocusManageResult focusManageResult);
        Task<Location> GetLastLocationAsync(string userId);
        Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location, DateTimeOffset now, FocusManageResult focusManageResult);
    }
}
