using System;

namespace Digit.Focus.Models
{
    public class FocusItem
    {
        public string Id { get; set; }
        public DateTimeOffset IndicateTime { get; set; }
        [Obsolete]
        public string DirectionsKey { get; set; }
        public Directions Directions { get; set; }
        public string CalendarEventId { get; set; }
        public string CalendarEventFeedId { get; set; }
        public string CalendarEventHash { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
