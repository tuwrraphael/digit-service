using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Service;
using DigitPushService.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{
    public class PushSyncService : IPushSyncService
    {
        private readonly IPushSyncStore pushSyncStore;
        private readonly IDigitPushServiceClient _digitPushServiceClient;
        private readonly IFocusStore _focusStore;

        public PushSyncService(IPushSyncStore pushSyncStore, IDigitPushServiceClient digitPushServiceClient,
            IFocusStore focusStore)
        {
            this.pushSyncStore = pushSyncStore;
            _digitPushServiceClient = digitPushServiceClient;
            _focusStore = focusStore;
        }

        public async Task<SyncAction[]> GetPendingSyncActions(string userId, DateTimeOffset now)
        {
            var syncActions = (await pushSyncStore.GetPendingSyncActions(userId)).ToList();
            if ((await _focusStore.GetActiveAsync(userId)).Any(v => null != v.DirectionsMetadata &&
            v.DirectionsMetadata.TravelStatus != Focus.Models.TravelStatus.Finished))
            {
                if (!syncActions.Any(s => s.Id == "locationSync"))
                {
                    syncActions.Add(new SyncAction()
                    {
                        Id = "locationSync",
                        Deadline = now
                    });
                }
            }
            return syncActions.ToArray();
        }

        private async Task Push(string userId)
        {
            await _digitPushServiceClient[userId].DigitSync.Location(new LocationSyncRequest());
        }

        public async Task<SyncResult> RequestLocationSync(string userId, DateTimeOffset now, DateTimeOffset deadline)
        {
            var all = await pushSyncStore.GetPendingSyncActions(userId);
            await pushSyncStore.AddSyncAction(userId, "locationSync", deadline);
            var pending = all.Where(d => now - d.Deadline <= DeviceSyncConstants.PushMissed).ToArray();
            var pendingAction = pending.Where(v => v.Id == "locationSync");
            if (!pending.Any())
            {
                await Push(userId);
                return new SyncResult()
                {
                    SyncRequested = true,
                    SyncPendingFor = null
                };
            }
            var nextPending = pending.OrderBy(v => v.Deadline).FirstOrDefault();
            if (!pending.Where(v => v.Deadline <= deadline).Any())
            {
                await Push(userId);
                return new SyncResult()
                {
                    SyncRequested = true,
                    SyncPendingFor = nextPending.Deadline
                };
            }
            return new SyncResult()
            {
                SyncRequested = false,
                SyncPendingFor = nextPending.Deadline
            };
        }

        public async Task SetDone(string userId, string id)
        {
            await pushSyncStore.SetDone(userId, id);
        }

        public async Task SetLocationRequestedExternal(string userId, DateTimeOffset nextUpdateAt)
        {
            var actions = await pushSyncStore.GetPendingSyncActions(userId);
            if (actions.Any(v => v.Id == "locationSync" && v.Deadline <= nextUpdateAt))
            {
                return;
            }
            await pushSyncStore.AddSyncAction(userId, "locationSync", nextUpdateAt);
        }

        public async Task SetLocationRequestDone(string userId)
        {
            await SetDone(userId, "locationSync");
        }
    }
}
