namespace DigitService.Models
{
    public class UserInformation
    {
        public bool PushChannelRegistered { get; set; }
        public bool CalendarReminderActive { get; set; }
        public string[] DeviceIds { get; set; }
    }
}