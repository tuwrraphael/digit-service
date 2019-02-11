using CalendarService.Models;
using System;
using TravelService.Models.Directions;

namespace DigitService.Models
{
    public class FocusDeparture
    {
        public Event Event { get; set; }
        public Route Route { get; set; }
        public DateTimeOffset DepartureTime { get; set; }
    }
}
