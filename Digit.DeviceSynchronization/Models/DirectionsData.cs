using System;

namespace Digit.DeviceSynchronization.Models
{
    public class DirectionsData
    {
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public LegData[] Legs { get; set; }
    }
}
