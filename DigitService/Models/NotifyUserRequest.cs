using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Models
{
    public class NotifyUserRequest
    {
        public string Message { get; set; }
        public string UserId { get; set; }
    }
}
