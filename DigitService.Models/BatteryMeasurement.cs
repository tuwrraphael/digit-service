using System;

namespace DigitService.Models
{
    public class BatteryMeasurement
    {
        public uint RawValue { get; set; }
        public DateTime MeasurementTime { get; set; }
    }
}