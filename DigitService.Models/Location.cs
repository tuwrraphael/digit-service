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
        public DateTime Timestamp { get; set; }
    }
}
