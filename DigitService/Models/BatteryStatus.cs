using System;

namespace DigitService.Models
{
    public class BatteryStatus
    {
        public BatteryStatus()
        {

        }

        public BatteryStatus(BatteryMeasurement batteryMeasurement,
            DateTime? lastChargedTime,
            DeviceBatteryCharacteristics deviceBatteryCharacteristics)
        {
            RawValue = batteryMeasurement.RawValue;
            LastMeasurementTime = batteryMeasurement.MeasurementTime;
            LastChargedTime = lastChargedTime;
            Voltage = (batteryMeasurement.RawValue / 1023.0) * deviceBatteryCharacteristics.MeasurmentRange;
            StateOfCharge = (Voltage - deviceBatteryCharacteristics.CutOffVoltage)
                / (deviceBatteryCharacteristics.MaxVoltage - deviceBatteryCharacteristics.CutOffVoltage);
        }

        public double Voltage { get; set; }
        public double StateOfCharge { get; set; }
        public uint RawValue { get; set; }
        public DateTime LastMeasurementTime { get; set; }
        public DateTime? LastChargedTime { get; set; }
    }
}