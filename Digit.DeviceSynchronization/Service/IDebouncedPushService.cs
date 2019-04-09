using System.Threading.Tasks;
using Digit.DeviceSynchronization.Models;

namespace Digit.DeviceSynchronization.Service
{
    public interface IDebouncedPushService
    {
        Task PushDebounced(string userId, ISyncRequest syncRequest);
    }
}