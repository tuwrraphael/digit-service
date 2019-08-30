using System;
using System.Collections.Generic;

namespace Digit.DeviceSynchronization.Models
{
    public class SyncAction
    {
        public string Id { get; set; }
        public DateTimeOffset? Deadline { get; set; }
    }
}