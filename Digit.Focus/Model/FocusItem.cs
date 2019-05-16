using CalendarService.Models;
using System;
using TravelService.Models.Directions;

namespace Digit.Focus.Models
{
    public class FocusItem
    {
        public string Id { get; set; }
        public DateTimeOffset IndicateTime { get; set; }
        [Obsolete]
        public string DirectionsKey => Directions?.Key;
        public DirectionsMetadata Directions { get; set; }
        public string CalendarEventId { get; set; }
        public string CalendarEventFeedId { get; set; }
        public string CalendarEventHash { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }

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
