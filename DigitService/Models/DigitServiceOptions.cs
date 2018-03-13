﻿namespace DigitService.Models
{
    public class DigitServiceOptions
    {
        public string CalendarServiceUrl { get; set; }
        public string DigitClientId { get; set; }
        public string DigitClientSecret { get; set; }
        public string ReminderCallbackUri { get; set; }
        public string ReminderMaintainanceCallbackUri { get; set; }
    }
}
