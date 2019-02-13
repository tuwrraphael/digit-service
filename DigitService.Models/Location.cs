using System;

namespace DigitService.Models
{
    public class Location
    {
        public Location()
        {

        }
        public Location(Location location)
        {
            Latitude = location.Latitude;
            Longitude = location.Longitude;
            Accuracy = location.Accuracy;
            Timestamp = location.Timestamp;
        }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }

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
