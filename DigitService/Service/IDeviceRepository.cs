using DigitService.Models;
using System;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IDeviceRepository
    {
        Task StoreBatteryMeasurementAsync(string deviceId, BatteryMeasurement batteryStatus);
        Task<BatteryMeasurement> GetLastBatteryMeasurementAsync(string deviceId);
        Task<Device> GetAsync(string id);
        Task<Device> CreateAsync(string id);
        Task<DateTime?> GetLastCharged(string deviceId);
        Task<DeviceBatteryCharacteristics> GetBatteryCharacteristics(string deviceId);
        Task ClaimAsync(string id, string userId);
        Task<bool> BelongsToAsync(string deviceId, string userId);
        Task<Device[]> GetDevices(string userId);
    }
}