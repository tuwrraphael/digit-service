using Digit.DeviceSynchronization.Models;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    public interface IDeviceDataService
    {
        Task<DeviceData> GetDeviceData(string userId, string deviceId);
        Task<DeviceSyncStatus> GetDeviceSyncStatus(string userId, string deviceId);
    }
}
