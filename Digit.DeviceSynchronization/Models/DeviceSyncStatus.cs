using System;

namespace Digit.DeviceSynchronization.Models
{
    public class DeviceSyncStatus
    {
        public bool UpToDate { get; set; }
        public DateTimeOffset? LastSyncTime { get; set; }
    }
}
