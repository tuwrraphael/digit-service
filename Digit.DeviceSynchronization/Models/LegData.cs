using System;

namespace Digit.DeviceSynchronization.Models
{
    public class LegData
    {
        public DateTimeOffset DepartureTime { get; set; }
        public string Line { get; set; }
        public string DepartureStop { get; set; }
        public string ArrivalStop { get; set; }
        public string Direction { get; set; }
    }
}
