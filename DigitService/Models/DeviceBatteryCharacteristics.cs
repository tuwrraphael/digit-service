namespace DigitService.Models
{
    public class DeviceBatteryCharacteristics
    {
        public double CutOffVoltage { get; set; }
        public double MaxVoltage { get; set; }
        public double MeasurmentRange { get; internal set; }
    }
}