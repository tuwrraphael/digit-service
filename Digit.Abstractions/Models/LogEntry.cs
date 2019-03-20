using System;

namespace Digit.Abstractions.Models
{
    public class LogEntry
    {
        public string Id { get; set; }
        public DateTime OccurenceTime { get; set; }
        public DateTime LogTime { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
    }
}