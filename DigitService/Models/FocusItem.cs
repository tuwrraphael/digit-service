using System;

namespace DigitService.Models
{
    public class FocusItem
    {
        public string Id { get; set; }
        public DateTimeOffset IndicateTime { get; set; }
        public string DirectionsKey { get; set; }
        public string CalendarEventId { get; set; }
        public string CalendarEventFeedId { get; set; }
    }
}
