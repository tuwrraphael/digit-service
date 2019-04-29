using CalendarService.Models;
using Digit.Focus.Models;
using System;
using TravelService.Models.Directions;

namespace DigitService.Models
{
    public class Plan
    {
        public Event[] Events { get; set; }
        public DateTimeOffset? GetUp { get; set; }
        public FocusItem[] FocusItems { get; set; }
        public DirectionsResult[] Directions { get; set; }
    }
}
