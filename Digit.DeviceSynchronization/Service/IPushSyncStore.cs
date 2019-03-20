using Digit.DeviceSynchronization.Models;
using System;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    public interface IPushSyncStore
    {
        Task<SyncAction[]> GetPendingSyncActions(string userId);
        Task AddSyncAction(string userId, string actionId, DateTimeOffset syncTime);
        Task SetDone(string userId, string actionId);
    }
}
