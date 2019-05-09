using System;

namespace DigitService.Models
{
    public class LocationResponse
    {
        public DateTimeOffset? NextUpdateRequiredAt { get; set; }
        public GeofenceRequest RequestGeofence { get; set; }
    }

    public class GeofenceRequest
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
