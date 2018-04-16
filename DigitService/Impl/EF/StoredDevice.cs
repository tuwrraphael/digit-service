using DigitService.Models;
using System;
using System.Collections.Generic;

namespace DigitService.Impl.EF
{
    public class StoredDevice
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public double BatteryCutOffVoltage { get; set; }
        public double BatteryMaxVoltage { get; set; }
        public double BatteryMeasurmentRange { get; set; }
        public List<StoredBatteryMeasurement> BatteryMeasurements { get; set; }
        public User User { get; internal set; }
    }

    public class StoredBatteryMeasurement
    {
        public string Id { get; set; }
        public string DeviceId { get; set; }
        public DateTime MeasurementTime { get; set; }
        public uint RawValue { get; set; }
        public StoredDevice Device { get; internal set; }
    }
}
