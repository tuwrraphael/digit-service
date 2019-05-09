using CalendarService.Models;
using System;
using TravelService.Models.Directions;

namespace Digit.Focus.Model
{
    public class FocusDeparture
    {
        public Event Event { get; set; }
        public Route Route { get; set; }
        public DateTimeOffset DepartureTime { get; set; }
    }
}
