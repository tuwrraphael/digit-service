namespace DigitService.Client
{
    public interface IDeviceCollection
    {
        IDevice this[string deviceId] { get; }
    }
}
