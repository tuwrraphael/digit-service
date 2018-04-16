using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IDeviceService
    {
        Task AddBatteryMeasurementAsync(string deviceId, BatteryMeasurement batteryMeasurement);
        Task<DeviceStatus> GetDeviceStatusAsync(string deviceId);
        Task<bool> HasAccessAsync(string deviceId, string userId);
        Task<bool> ClaimAsync(string userId, string id);
        Task<DeviceStatus[]> GetDevices(string v);
    }
}
