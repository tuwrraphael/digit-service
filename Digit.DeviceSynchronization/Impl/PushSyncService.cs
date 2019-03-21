using Digit.Abstractions.Service;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using DigitPushService.Client;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{

    public class PushSyncService : IPushSyncService
    {
        private readonly IPushSyncStore pushSyncStore;
        private readonly IDigitPushServiceClient digitPushServiceClient;
        private readonly IDigitLogger logger;

        public PushSyncService(IPushSyncStore pushSyncStore,
            IDigitPushServiceClient digitPushServiceClient, IDigitLogger logger)
        {
            this.pushSyncStore = pushSyncStore;
            this.digitPushServiceClient = digitPushServiceClient;
            this.logger = logger;
        }

        public async Task<SyncAction[]> GetPendingSyncActions(string userId, DateTimeOffset now)
        {
            return await pushSyncStore.GetPendingSyncActions(userId);
        }

        private async Task Push(string userId, ISyncRequest syncRequest)
        {
            try
            {
                await digitPushServiceClient.Push[userId].Create(new DigitPushService.Models.PushRequest()
                {
                    Options = new PushServer.Models.PushOptions()
                    {
                        Urgency = PushServer.Models.PushUrgency.High
                    },
                    ChannelOptions = syncRequest.GetChannelOptions(),
                    Payload = syncRequest.GetPayload()
                });
                await logger.Log(userId, $"Pushed {syncRequest}", 1);
            }
            catch (PushChannelNotFoundException)
            {
                await logger.Log(userId, $"Could not find channel for {syncRequest}", 3);
            }
            catch (Exception e)
            {
                await logger.Log(userId, $"Could not push {syncRequest}; Error: {e.Message}", 3);
            }
        }

        public async Task<SyncResult> RequestSync(string userId, ISyncRequest syncRequest, DateTimeOffset now)
        {
            var all = await pushSyncStore.GetPendingSyncActions(userId);
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
            await pushSyncStore.SetDone(userId, syncRequest.Id);
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
