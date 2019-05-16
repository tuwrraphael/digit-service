using System;
using System.Collections.Generic;
using DigitService.Impl.EF;

namespace DigitService.Models
{
    public class User
    {
        public string Id { get; set; }
        public string ReminderId { get; set; }
        public List<StoredDevice> Devices { get; set; }
        public int? StoredLocationId { get; set; }
        public StoredLocation StoredLocation { get; set; }
        public List<StoredFocusItem> FocusItems { get; set; }
        [Obsolete]
        public DateTime? GeofenceFrom { get; set; }
        [Obsolete]
        public DateTime? GeofenceTo { get; set; }
        public string ActiveFocusItem { get; set; }
    }
}
