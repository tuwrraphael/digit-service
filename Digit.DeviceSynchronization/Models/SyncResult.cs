using System;

namespace Digit.DeviceSynchronization.Models
{
    public class SyncResult
    {
        public bool SyncRequested { get; set; }
        public DateTimeOffset? SyncPendingFor { get; set; }
    }
}