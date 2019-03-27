using System;

namespace Digit.DeviceSynchronization.Models
{
    public class DeviceSyncStatus
    {
        public string DeviceId { get; set; }
        public bool UpToDate { get; set; }
        public DateTimeOffset? LastSyncTime { get; set; }
    }
}
