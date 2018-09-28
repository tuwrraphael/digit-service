using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IPushService
    {
        Task Push(string user, PushPayload payload);
        Task Push(string user, string payload);
        Task RegisterUser(string user, string registrationId);
        Task<PushRegistrationType> GetPushRegistrationType(string user);
    }
}
