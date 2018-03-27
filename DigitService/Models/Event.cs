using Newtonsoft.Json;
using System;

namespace DigitService
{
    public class Event
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Subject { get; set; }
        public string Location { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsAllDay { get; set; }
        public string Id { get; set; }
        public string FeedId { get; internal set; }
    }
}