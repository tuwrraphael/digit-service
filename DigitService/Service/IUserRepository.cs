using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IUserRepository
    {
        Task CreateUser(NewUser user);
        Task<bool> Exists(string userId);
        Task RegisterPushChannel(string userId, string registrationId);
        Task<DeviceClaimResult> ClaimDevice(string userId, string deviceId);
    }
}
