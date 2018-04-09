using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IUserService
    {
        Task<UserInformation> GetInformationAsync(string userId);
        Task<UserInformation> CreateAsync(string userId);
        Task<UserInformation> MaintainAsync(string userId);
        Task<string> GetUserIdForReminderAsync(string reminderId);
        Task RenewReminder(string userId, RenewReminderRequest request);
        Task RegisterPushChannel(string userId, string channelId);
    }
}
