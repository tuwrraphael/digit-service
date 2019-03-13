using Digit.DeviceSynchronization.Models;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    public interface IDeviceSynchronization
    {
        /// <exception cref="DeviceClaimedException">the device was claimed by another user</exception>
        Task RequestSynchronizationAsync(string userId, string deviceId, DeviceSyncRequest request);
        Task TriggerSynchronizationAsync(string userId);
        Task<DeviceSyncStatus> GetDeviceSyncStatusAsync(string userId, string deviceId);
        Task<DeviceData> GetDeviceDataAsync(string userId, string deviceData);
        Task RemoveDeviceSync(string userId, string deviceId);
    }
}
