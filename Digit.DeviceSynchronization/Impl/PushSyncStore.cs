using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{
    internal class PushSyncStore : IPushSyncStore
    {
        private readonly DeviceSynchronizationContext deviceSynchronizationContext;

        internal PushSyncStore(DeviceSynchronizationContext deviceSynchronizationContext)
        {
            this.deviceSynchronizationContext = deviceSynchronizationContext;
        }

        public async Task AddSyncAction(string userId, string actionId, DateTimeOffset syncTime)
        {
            await deviceSynchronizationContext.AddAsync(new StoredSyncAction()
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                ActionId = actionId,
                Done = false,
                RequestedFor = syncTime.UtcDateTime
            });
        }

        public async Task<SyncAction[]> GetPendingSyncActions(string userId)
        {
            return await deviceSynchronizationContext.SyncActions.Where(v => v.UserId == userId && !v.Done)
                .Select(v => new SyncAction()
                {
                    Id = v.ActionId,
                    Deadline = v.RequestedFor
                })
                .ToArrayAsync();
        }

        public async Task SetDone(string userId, string actionId)
        {
            var actions = await deviceSynchronizationContext.SyncActions.Where(v => v.UserId == userId && v.ActionId == actionId).ToArrayAsync();
            foreach (var action in actions)
            {
                action.Done = true;
            }
            await deviceSynchronizationContext.SaveChangesAsync();
        }
    }
}
