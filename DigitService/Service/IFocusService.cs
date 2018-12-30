using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IFocusService
    {
        Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location);
        Task ReminderDeliveryAsync(string userId, ReminderDelivery reminderDelivery);
        Task NotifyCallbackAsync(NotifyUserRequest request);
    }
}
