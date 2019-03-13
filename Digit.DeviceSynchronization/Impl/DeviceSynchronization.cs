using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Service;
using DigitPushService.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{
    public class DeviceSynchronization : IDeviceSynchronization
    {
        internal DeviceSynchronization(IFocusStore focusStore,
            IDigitPushServiceClient digitPushServiceClient, IDeviceSyncStore deviceSyncStore,
            IFocusItemDigest focusItemDigest)
        {

        }

        public Task<DeviceData> GetDeviceDataAsync(string userId, string deviceData)
        {
            throw new NotImplementedException();
        }

        public Task<DeviceSyncStatus> GetDeviceSyncStatusAsync(string userId, string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveDeviceSync(string userId, string deviceId)
        {
            throw new NotImplementedException();
        }

        public Task RequestSynchronizationAsync(string userId, string deviceId, DeviceSyncRequest request)
        {
            throw new NotImplementedException();
        }

        public Task TriggerSynchronizationAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }
}
