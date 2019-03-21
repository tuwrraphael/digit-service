using System;

namespace Digit.DeviceSynchronization.Models
{
    public class Device
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public bool UpToDate { get; set; }
        public DateTimeOffset LastSyncTime { get; set; }
        public string FocusItemId { get; set; }
        public string FocusItemDigest { get; set; }
    }
}
