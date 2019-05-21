﻿using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{
    public class PushSyncService : IPushSyncService
    {
        private readonly IPushSyncStore pushSyncStore;
        private readonly IDebouncedPushService _debouncedPushService;
        private readonly IFocusStore _focusStore;

        public PushSyncService(IPushSyncStore pushSyncStore, IDebouncedPushService debouncedPushService,
            IFocusStore focusStore)
        {
            this.pushSyncStore = pushSyncStore;
            _debouncedPushService = debouncedPushService;
            _focusStore = focusStore;
        }

        public async Task<SyncAction[]> GetPendingSyncActions(string userId, DateTimeOffset now)
        {
            var syncActions = (await pushSyncStore.GetPendingSyncActions(userId)).ToList();
            if ((await _focusStore.GetActiveAsync(userId)).Any(v => null != v.DirectionsMetadata &&
            v.DirectionsMetadata.TravelStatus != Focus.Models.TravelStatus.Finished))
            {
                var locationSyncRequest = new LocationPushSyncRequest(now);
                if (!syncActions.Any(s => s.Id == locationSyncRequest.Id))
                {
                    syncActions.Add(new SyncAction()
                    {
                        Id = locationSyncRequest.Id,
                        Deadline = now
                    });
                }
            }
            return syncActions.ToArray();
        }

        private async Task Push(string userId, ISyncRequest syncRequest)
        {
            await _debouncedPushService.PushDebounced(userId, syncRequest);
        }

        public async Task<SyncResult> RequestSync(string userId, ISyncRequest syncRequest, DateTimeOffset now)
        {
            var all = await pushSyncStore.GetPendingSyncActions(userId);
            await pushSyncStore.AddSyncAction(userId, syncRequest.Id, syncRequest.Deadline);
            var pending = all.Where(d => now - d.Deadline <= syncRequest.AllowMissed).ToArray();
            var pendingAction = pending.Where(v => v.Id == syncRequest.Id);
            if (!pending.Any())
            {
                await Push(userId, syncRequest);
                return new SyncResult()
                {
                    SyncRequested = true,
                    SyncPendingFor = null
                };
            }
            var nextPending = pending.OrderBy(v => v.Deadline).FirstOrDefault();
            if (!pending.Where(v => v.Deadline <= syncRequest.Deadline).Any())
            {
                await Push(userId, syncRequest);
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

        public async Task SetDone(string userId, ISyncRequest syncRequest)
        {
            await SetDone(userId, syncRequest.Id);
        }

        public async Task SetDone(string userId, string id)
        {
            await pushSyncStore.SetDone(userId, id);
        }

        public async Task SetRequestedExternal(string userId, ISyncRequest syncRequest)
        {
            var actions = await pushSyncStore.GetPendingSyncActions(userId);
            if (actions.Any(v => v.Id == syncRequest.Id && v.Deadline <= syncRequest.Deadline))
            {
                return;
            }
            await pushSyncStore.AddSyncAction(userId, syncRequest.Id, syncRequest.Deadline);
        }
    }
}
