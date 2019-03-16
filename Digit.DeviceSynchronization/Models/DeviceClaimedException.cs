using System;

namespace Digit.DeviceSynchronization.Models
{
    public class DeviceClaimedException : Exception
    {
        public DeviceClaimedException(string deviceId, string userId)
            : base($"Device {deviceId} already claimed by {userId}")
        {

        }
    }
}
