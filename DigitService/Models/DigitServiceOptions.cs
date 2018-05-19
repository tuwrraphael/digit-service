namespace DigitService.Models
{
    public class DigitServiceOptions
    {
        public string DigitClientId { get; set; }
        public string DigitClientSecret { get; set; }
        public string ReminderCallbackUri { get; set; }
        public string ReminderMaintainanceCallbackUri { get; set; }
        public object ServiceIdentityUrl { get; internal set; }
    }
}
