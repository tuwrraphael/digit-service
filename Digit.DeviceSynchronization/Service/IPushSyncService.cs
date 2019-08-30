using Digit.DeviceSynchronization.Models;
using System;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Service
{
    public interface IPushSyncService
    {
        [Obsolete]
        Task<SyncAction[]> GetPendingSyncActions(string userId, DateTimeOffset now);
        Task<SyncResult> RequestLocationSync(string userId, DateTimeOffset now, DateTimeOffset deadline);
        Task SetLocationRequestedExternal(string userId, DateTimeOffset nextUpdateAt);
        Task SetLocationRequestDone(string userId);
        [Obsolete]
        Task SetDone(string userId, string id);
    }
}
