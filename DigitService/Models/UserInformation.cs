using Newtonsoft.Json;

namespace DigitService.Models
{
    public class UserInformation
    {
        public bool PushChannelRegistered { get; set; }
        public bool CalendarReminderActive { get; set; }
    }
}