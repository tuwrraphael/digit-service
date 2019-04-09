using System;
using System.Collections.Generic;

namespace Digit.DeviceSynchronization.Models
{
    public class SyncAction
    {
        public string Id { get; set; }
        public DateTimeOffset? Deadline { get; set; }
    }

    public interface ISyncRequest
    {
        string Id { get; }
        Dictionary<string, string> GetChannelOptions();
        DateTimeOffset Deadline { get; }
        TimeSpan AllowMissed { get; }
    }

    public class SyncRequestBase
    {
        public DateTimeOffset Deadline { get; }
        public TimeSpan AllowMissed { get; } = DeviceSyncConstants.PushMissed;

        public SyncRequestBase(DateTimeOffset deadline)
        {
            Deadline = deadline;
        }
    }

    public class DevicePushSyncRequest : SyncRequestBase, ISyncRequest
    {
        private readonly string deviceId;

        public DevicePushSyncRequest(string deviceId, DateTimeOffset deadline) :
            base(deadline)
        {
            this.deviceId = deviceId;
        }

        public string Id => $"deviceSync.{deviceId}";

        public Dictionary<string, string> GetChannelOptions()
        {
            return new Dictionary<string, string>()
            {
                { $"digit.sync.{deviceId}", null }
            };
        }

        public override string ToString()
        {
            return $"DeviceSync: {deviceId}";
        }
    }

    public class LocationPushSyncRequest : SyncRequestBase, ISyncRequest
    {
        public LocationPushSyncRequest(DateTimeOffset deadline) :
            base(deadline)
        {
        }

        public string Id => "locationSync";

        public Dictionary<string, string> GetChannelOptions()
        {
            return new Dictionary<string, string>()
            {
                { "digitLocationRequest", null }
            };
        }

        public override string ToString()
        {
            return $"Sync location";
        }
    }
}