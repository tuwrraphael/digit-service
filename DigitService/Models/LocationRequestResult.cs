using System;

namespace DigitService.Models
{
    public class LocationRequestResult
    {
        public bool LocationRequestSent { get; set; }
        public DateTimeOffset? LocationRequestTime { get; set; }
    }
}
