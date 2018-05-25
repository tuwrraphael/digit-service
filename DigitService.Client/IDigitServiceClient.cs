using DigitService.Models;
using System;
using System.Threading.Tasks;

namespace DigitService.Client
{
    public interface IDigitServiceClient
    {
        Task<bool> LogAsync(string message, int code = 0, DateTime? occurenceTime = null);
        Task SetupPushChannelAsync(PushChannelRegistration channelRegistration);

        IDeviceCollection Device { get; }

        ILocation Location { get; }
    }
}
