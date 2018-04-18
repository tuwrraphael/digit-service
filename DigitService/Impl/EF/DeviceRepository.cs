using DigitService.Models;
using DigitService.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl.EF
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly DigitServiceContext digitServiceContext;

        public DeviceRepository(DigitServiceContext digitServiceContext)
        {
            this.digitServiceContext = digitServiceContext;
        }

        public async Task<bool> BelongsToAsync(string deviceId, string userId)
        {
            return await digitServiceContext.Devices.Where(v => v.Id == deviceId && v.UserId == userId).AnyAsync();
        }

        public async Task ClaimAsync(string id, string userId)
        {
            var device = await digitServiceContext.Devices.SingleAsync(v => v.Id == id);
            device.UserId = userId;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task<Device> CreateAsync(string id)
        {
            var storedDevice = new StoredDevice()
            {
                Id = id,
                BatteryCutOffVoltage = 2.5,
                BatteryMaxVoltage = 3.4,
                BatteryMeasurmentRange = 3.6
            };
            await digitServiceContext.Devices.AddAsync(storedDevice);
            await digitServiceContext.SaveChangesAsync();
            return new Device()
            {
                UserId = storedDevice.UserId,
                Id = storedDevice.Id
            };
        }

        public async Task<Device> GetAsync(string id)
        {
            return await digitServiceContext.Devices.Where(v => v.Id == id).Select(device =>
                new Device()
                {
                    UserId = device.UserId,
                    Id = device.Id
                }).SingleOrDefaultAsync();
        }

        public async Task<DeviceBatteryCharacteristics> GetBatteryCharacteristics(string deviceId)
        {
            return await digitServiceContext.Devices.Where(v => v.Id == deviceId).Select(device =>
                new DeviceBatteryCharacteristics()
                {
                    CutOffVoltage = device.BatteryCutOffVoltage,
                    MaxVoltage = device.BatteryMaxVoltage,
                    MeasurmentRange = device.BatteryMeasurmentRange
                }).SingleAsync();
        }

        public async Task<BatteryMeasurement> GetLastBatteryMeasurementAsync(string deviceId)
        {
            return await digitServiceContext.BatteryMeasurements.Where(v => v.DeviceId == deviceId)
                .OrderByDescending(v => v.MeasurementTime).Select(v => new BatteryMeasurement()
                {
                    MeasurementTime = v.MeasurementTime,
                    RawValue = v.RawValue
                }).FirstOrDefaultAsync();
        }

        public async Task<DateTime?> GetLastCharged(string deviceId)
        {
            var measurements = await digitServiceContext.BatteryMeasurements.Where(v => v.DeviceId == deviceId)
                 .OrderByDescending(v => v.MeasurementTime).ToArrayAsync();
            if (measurements.Length == 0)
            {
                return null;
            }
            var before = uint.MaxValue;
            foreach (var m in measurements)
            {
                if (m.RawValue > before)
                {
                    return m.MeasurementTime;
                }
                before = m.RawValue;
            }
            return measurements.Last().MeasurementTime;
            //if (last == null)
            //{
            //    return null;
            //}
            //var lastLower = await digitServiceContext.BatteryMeasurements.Where(v => v.DeviceId == deviceId)
            //     .Where(v => v.RawValue < last.RawValue)
            //      .OrderByDescending(v => v.MeasurementTime).FirstOrDefaultAsync();
            //StoredBatteryMeasurement firstAfterCharge;
            //if (lastLower == null)
            //{
            //    firstAfterCharge = await digitServiceContext.BatteryMeasurements.Where(v => v.DeviceId == deviceId)
            //        .OrderBy(v => v.MeasurementTime)
            //        .FirstOrDefaultAsync();
            //}
            //else
            //{
            //    firstAfterCharge = await digitServiceContext.BatteryMeasurements.Where(v => v.DeviceId == deviceId)
            //        .Where(v => v.MeasurementTime > lastLower.MeasurementTime).OrderByDescending(v => v.RawValue)
            //        .FirstOrDefaultAsync();
            //}
            //if (firstAfterCharge == null)
            //{
            //    return null;
            //}
            //return firstAfterCharge.MeasurementTime;
        }

        public async Task StoreBatteryMeasurementAsync(string deviceId, BatteryMeasurement batteryStatus)
        {
            var mes = new StoredBatteryMeasurement()
            {
                DeviceId = deviceId,
                RawValue = batteryStatus.RawValue,
                MeasurementTime = batteryStatus.MeasurementTime
            };
            await digitServiceContext.BatteryMeasurements.AddAsync(mes);
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task<Device[]> GetDevices(string userId)
        {
            return await digitServiceContext.Devices.Where(v => v.UserId == userId).
                Select(v => new Device()
                {
                    UserId = v.UserId,
                    Id = v.Id
                }).ToArrayAsync();
        }
    }
}
