using Digit.DeviceSynchronization.Models;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    internal interface IDeviceSyncStore
    {
        Task CreateAsync(string userId, string deviceId, DeviceSyncRequest request);
        Task<Device[]> GetForUserAsync(string userId);
        Task<string> DeviceClaimedByAsync(string deviceId);
    }
}
