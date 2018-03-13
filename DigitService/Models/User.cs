using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Models
{
    public class User
    {
        public string Id { get; set; }
        public string PushChannel { get; set; }
        public string ReminderId { get; set; }
    }
}
