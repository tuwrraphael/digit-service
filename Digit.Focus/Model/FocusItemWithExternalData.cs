using CalendarService.Models;
using System;
using TravelService.Models.Directions;

namespace Digit.Focus.Models
{
    public class FocusItemWithExternalData
    {
        public string Id { get; set; }
        public DateTimeOffset IndicateTime { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public Event CalendarEvent { get; set; }
        public TransitDirections Directions { get; set; }
        public DirectionsMetadata DirectionsMetadata { get; set; }
        // legacy properties
        [Obsolete]
        public string CalendarEventId => CalendarEvent?.Id;
        [Obsolete]
        public string CalendarEventFeedId => CalendarEvent?.FeedId;
        [Obsolete]
        public string DirectionsKey => DirectionsMetadata?.Key;
    }
}
