using System.Collections.Generic;
using DigitService.Impl.EF;

namespace DigitService.Models
{
    public class User
    {
        public string Id { get; set; }
        public string PushChannel { get; set; }
        public string ReminderId { get; set; }
        public List<StoredDevice> Devices { get; set; }
    }
}
