using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Models
{
    public class ReminderDelivery
    {
        public string ReminderId { get; set; }
        public Event Event { get; set; }
        public string ClientState { get; set; }
    }
}
