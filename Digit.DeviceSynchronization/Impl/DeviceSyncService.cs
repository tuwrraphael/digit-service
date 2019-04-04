using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using DigitPushService.Client;
using PushServer.PushConfiguration.Abstractions.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{

    public class DeviceSyncService : IDeviceSyncService
    {
        private readonly IDigitPushServiceClient digitPushServiceClient;
        private readonly IDeviceSyncStore deviceSyncStore;

        internal DeviceSyncService(IDigitPushServiceClient digitPushServiceClient,
            IDeviceSyncStore deviceSyncStore)
        {
            this.digitPushServiceClient = digitPushServiceClient;
            this.deviceSyncStore = deviceSyncStore;
        }

        public async Task RequestSynchronizationAsync(string userId, string deviceId, DeviceSyncRequest request)
        {
            var claimedBy = await deviceSyncStore.DeviceClaimedByAsync(deviceId);
            var channels = await digitPushServiceClient.PushChannels[userId].GetAllAsync();
            var syncChannel = channels.Where(v => v.Id == request.PushChannelId).SingleOrDefault();
            if (null == syncChannel)
            {
                throw new PushChannelNotFoundException($"Push channel {request.PushChannelId} not assigned to user {userId}.");
            }
            if (null != claimedBy && userId != claimedBy)
            {
                throw new DeviceClaimedException(deviceId, userId);
            }
            if (null == claimedBy)
            {
                await deviceSyncStore.CreateAsync(userId, deviceId);
            }
            var deviceSyncKey = $"digit.sync.{deviceId}";
            foreach (var channel in channels.Where(v => v.Id != syncChannel.Id &&
                null != v.Options
                && v.Options.ContainsKey(deviceSyncKey)))
            {
                channel.Options.Remove(deviceSyncKey);
                await digitPushServiceClient.PushChannels[userId][channel.Id].Options.PutAsync(channel.Options);
            }
            if (syncChannel.Options == null || !syncChannel.Options.ContainsKey(deviceSyncKey))
            {
                var options = syncChannel.Options ?? new PushChannelOptions();
                options.Add(deviceSyncKey, null);
                await digitPushServiceClient.PushChannels[userId][syncChannel.Id].Options.PutAsync(options);
            }
        }
    }
}
