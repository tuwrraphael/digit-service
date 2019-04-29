using Digit.DeviceSynchronization.Models;
using System;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    public interface IPushSyncService
    {
        Task<SyncActions> GetPendingSyncActions(string userId, DateTimeOffset now);
        Task<SyncResult> RequestSync(string userId, ISyncRequest syncRequest, DateTimeOffset now);
        Task SetRequestedExternal(string userId, ISyncRequest syncRequest);
        Task SetDone(string userId, ISyncRequest syncRequest);
        Task SetDone(string userId, string id);
    }
}
