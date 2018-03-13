namespace DigitService.Models
{
    public class ReminderRequest
    {
        public uint Minutes { get; set; }
        public string NotificationUri { get; set; }
        public string ClientState { get; set; }
    }
}