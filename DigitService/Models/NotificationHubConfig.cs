using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Models
{
    public class NotificationHubConfig
    {
        public string HubName { get; set; }
        public string HubConnection { get; set; }
        public string HubNamespace { get; set; }
        public string HubSASKeyName { get; set; }
        public string HubSASKey { get; set; }
    }
}
