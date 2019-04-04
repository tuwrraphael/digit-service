using Digit.DeviceSynchronization.Models;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    public interface IDeviceSyncStore
    {
        Task CreateAsync(string userId, string deviceId);
        Task<Device[]> GetForUserAsync(string userId);
        Task<string> DeviceClaimedByAsync(string deviceId);
    }
}
