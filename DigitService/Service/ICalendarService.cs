using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface ICalendarService
    {
        Task<ReminderRegistration> RegisterReminder(string userId, uint minutes);
        Task<ReminderRegistration> RenewReminder(string userId, RenewReminderRequest request);
        Task<bool> ReminderAliveAsync(string userId, string reminderId);
    }
}
