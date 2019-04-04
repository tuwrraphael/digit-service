using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Models;
using Digit.Focus.Service;
using System;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{
    public class DeviceSyncFocusSubscriber : IFocusSubscriber
    {
        private readonly IPushSyncService pushSyncService;
        private readonly IDeviceSyncStore _deviceSyncStore;

        public DeviceSyncFocusSubscriber(IPushSyncService pushSyncService, IDeviceSyncStore deviceSyncStore)
        {
            this.pushSyncService = pushSyncService;
            _deviceSyncStore = deviceSyncStore;
        }

        public async Task ActiveItemChanged(string userId, FocusItem currentItem)
        {
            var devices = await _deviceSyncStore.GetForUserAsync(userId);
            foreach (var device in devices)
            {
                await pushSyncService.RequestSync(userId, new DevicePushSyncRequest(device.Id, DateTimeOffset.Now.AddMinutes(15)), DateTimeOffset.Now);
            }
        }

        public Task ActiveItemsChanged(string userId, FocusItem[] items)
        {
            return Task.CompletedTask;
        }
    }
}
