using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IUserRepository
    {
        Task<User> CreateUser(string userId);
        Task<User> GetAsync(string userId);
        Task StoreReminderIdAsync(string userId, string reminderId);
        Task<User> GetByReminder(string reminderId);
        Task<User> GetOrCreateAsync(string userId);
    }
}
