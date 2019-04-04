using System;

namespace Digit.DeviceSynchronization.Models
{
    public class DirectionsData
    {
        public DateTimeOffset DepartureTime { get; set; }
        public DateTimeOffset ArrivalTime { get; set; }
        public LegData[] Legs { get; set; }
    }
}
