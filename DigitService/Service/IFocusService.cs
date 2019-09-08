using CalendarService.Models;
using Digit.Focus.Model;
using Digit.Focus.Service;
using DigitService.Models;
using System.Threading.Tasks;
using TravelService.Models;

namespace DigitService.Service
{
    public interface IFocusService : IFocusPatchService
    {
        Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location);
        Task ReminderDeliveryAsync(string userId, ReminderDelivery reminderDelivery);
        Task NotifyCallbackAsync(NotifyUserRequest request);
        Task DirectionsCallbackAsync(DirectionsUpdate update);
    }
}
