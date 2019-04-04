using Digit.DeviceSynchronization.Models;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    public interface IDeviceSyncService
    {
        /// <exception cref="DeviceClaimedException">the device was claimed by another user</exception>
        Task RequestSynchronizationAsync(string userId, string deviceId, DeviceSyncRequest request);
    }
}
