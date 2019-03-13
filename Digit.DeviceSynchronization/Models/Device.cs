using System;
using System.Collections.Generic;
using System.Text;

namespace Digit.DeviceSynchronization.Models
{
    internal class Device
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public bool UpToDate { get; set; }
        public DateTimeOffset LastSyncTime { get; set; }
        public string FocusItemId { get; set; }
        public string FocusItemDigest { get; set; }
    }
}
