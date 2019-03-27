using System;

namespace Digit.DeviceSynchronization.Models
{
    public class DeviceAccessException : Exception
    {
        public DeviceAccessException(string deviceId, string userId, string ownerId)
            : base($"Device {deviceId} is not accessible by {userId} because its claimed by {ownerId}")
        {

        }
    }
}
