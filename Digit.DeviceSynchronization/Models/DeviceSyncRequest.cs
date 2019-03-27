using System.ComponentModel.DataAnnotations;

namespace Digit.DeviceSynchronization.Models
{
    public class DeviceSyncRequest
    {
        [Required]
        public string PushChannelId { get; set; }
    }
}
