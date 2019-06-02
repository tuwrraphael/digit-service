using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Digit.Abstractions.Models
{
    public class LogEntry
    {
        public string Id { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public DigitTraceAction DigitTraceAction { get; set; }
        public string UserId { get; set; }
        public string FocusItemId { get; set; }
        public IDictionary<string, string> AdditionalData { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
        public LogLevel LogLevel { get; set; }

        [Obsolete]
        public int Code => LogLevel == LogLevel.Error ? 3 : 0;
        [Obsolete]
        public DateTimeOffset OccurenceTime => Timestamp;
        [Obsolete]
        public DateTimeOffset LogTime => Timestamp;
    }

    public class LegacyLogRequest
    {
        public string Id { get; set; }
        public DateTime OccurenceTime { get; set; }
        public DateTime LogTime { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
    }
}