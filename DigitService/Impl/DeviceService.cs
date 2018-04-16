using DigitService.Models;
using DigitService.Service;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository deviceRepository;

        public DeviceService(IDeviceRepository deviceRepository)
        {
            this.deviceRepository = deviceRepository;
        }

        public async Task AddBatteryMeasurementAsync(string deviceId, BatteryMeasurement batteryMeasurement)
        {
            await deviceRepository.StoreBatteryMeasurementAsync(deviceId, batteryMeasurement);
        }

        public async Task<bool> ClaimAsync(string userId, string id)
        {
            var device = await deviceRepository.GetAsync(id);
            if (null == device)
            {
                device = await deviceRepository.CreateAsync(id);
            }
            if (device.UserId == userId)
            {
                return true;
            }
            if (device.UserId != null)
            {
                return false;
            }
            await deviceRepository.ClaimAsync(id, userId);
            return true;
        }

        public async Task<DeviceStatus[]> GetDevices(string v)
        {
            var devs = await deviceRepository.GetDevices(v);
            var tasks = devs.Select(a => GetDeviceStatusAsync(a.Id)).ToArray();
            return await Task.WhenAll(tasks);
        }

        public async Task<DeviceStatus> GetDeviceStatusAsync(string deviceId)
        {
            var batteryMeasurement = await deviceRepository.GetLastBatteryMeasurementAsync(deviceId);
            var lastCharged = await deviceRepository.GetLastCharged(deviceId);
            var characteristics = await deviceRepository.GetBatteryCharacteristics(deviceId);
            var battStatus = batteryMeasurement == null ? null : new BatteryStatus(batteryMeasurement, lastCharged, characteristics);
            return new DeviceStatus()
            {
                Battery = battStatus
            };
        }

        public async Task<bool> HasAccessAsync(string deviceId, string userId)
        {
            return await deviceRepository.BelongsToAsync(deviceId, userId);
        }
    }
}
