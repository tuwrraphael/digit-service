using Digit.DeviceSynchronization.Models;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    public interface IDeviceDataService
    {
        Task<DeviceData> GetDeviceData(string userId, string deviceData);
        Task<DeviceSyncStatus[]> GetDeviceSyncStatus(string userId);
    }
}
