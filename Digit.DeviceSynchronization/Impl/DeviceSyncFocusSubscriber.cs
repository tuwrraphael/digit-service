using Digit.Abstractions.Service;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Model;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitPushService.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{
    public class DeviceSyncFocusSubscriber : IFocusSubscriber
    {
        private readonly IDigitPushServiceClient _digitPushServiceClient;
        private readonly IDeviceSyncStore _deviceSyncStore;
        private readonly IDigitLogger _digitLogger;

        public DeviceSyncFocusSubscriber(IDigitPushServiceClient digitPushServiceClient, IDeviceSyncStore deviceSyncStore,
            IDigitLogger digitLogger)
        {
            _digitPushServiceClient = digitPushServiceClient;
            _deviceSyncStore = deviceSyncStore;
            _digitLogger = digitLogger;
        }

        public async Task ActiveItemChanged(string userId, FocusItemWithExternalData currentItem)
        {
            var devices = await _deviceSyncStore.GetForUserAsync(userId);
            foreach (var device in devices)
            {
                await _digitPushServiceClient[userId].DigitSync.Device(new DeviceSyncRequest()
                {
                    DeviceId = device.Id
                });
                await _digitLogger.LogForUser(userId, $"Requested device sync", Abstractions.Models.DigitTraceAction.RequestPush,
                    new Dictionary<string, object>() { { "deviceId", device.Id } });
            }
        }

        public Task ActiveItemsChanged(string userId, FocusManageResult res)
        {
            return Task.CompletedTask;
        }
    }
}
