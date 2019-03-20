using Digit.Abstractions.Service;
using Digit.DeviceSynchronization;
using Digit.DeviceSynchronization.Impl;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using DigitPushService.Client;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DigitService.Test
{
    public class PushSyncServiceTests
    {
        const string userId = "12345";

        private static readonly DateTimeOffset Now = new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static Mock<IPushSyncStore> MockPushSyncStore(SyncAction[] pending)
        {
            var pushSyncStoreMock = new Mock<IPushSyncStore>(MockBehavior.Strict);
            pushSyncStoreMock.Setup(v => v.GetPendingSyncActions(userId))
                .Returns(Task.FromResult(pending));
            pushSyncStoreMock.Setup(v => v.AddSyncAction(userId, It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            return pushSyncStoreMock;
        }

        private static IDigitLogger MockLogger()
        {
            return Mock.Of<IDigitLogger>();
        }

        private static IDigitPushServiceClient MockPushServiceClient()
        {
            return Mock.Of<IDigitPushServiceClient>();
        }

        public class RequestSync
        {


            [Fact]
            public async void NoPending_Requested()
            {
                var service = new PushSyncService(MockPushSyncStore(new SyncAction[] { }).Object, MockPushServiceClient(), MockLogger());
                var res = await service.RequestSync(userId, new LegacyLocationPushSyncRequest(Now), Now);
                Assert.True(res.SyncRequested);
                Assert.False(res.SyncPendingFor.HasValue);
            }

            [Fact]
            public async void PendingAfterDeadline_Requested()
            {
                var service = new PushSyncService(MockPushSyncStore(new[] { new SyncAction() {
                    Id = new LegacyLocationPushSyncRequest(Now).Id,
                    Deadline = Now.AddMinutes(30)
                } }).Object, MockPushServiceClient(), MockLogger());
                var res = await service.RequestSync(userId, new LegacyLocationPushSyncRequest(Now.AddMinutes(15)), Now);
                Assert.True(res.SyncRequested);
                Assert.Equal(Now.AddMinutes(30), res.SyncPendingFor);
            }

            [Fact]
            public async void PendingBeforeDeadline_NotRequested()
            {
                var service = new PushSyncService(MockPushSyncStore(new[] { new SyncAction() {
                    Id = new LegacyLocationPushSyncRequest(Now).Id,
                    Deadline = Now.AddMinutes(15)
                } }).Object, MockPushServiceClient(), MockLogger());
                var res = await service.RequestSync(userId, new LegacyLocationPushSyncRequest(Now.AddMinutes(30)), Now);
                Assert.False(res.SyncRequested);
                Assert.Equal(Now.AddMinutes(15), res.SyncPendingFor);
            }

            [Fact]
            public async void OtherActionPendingBeforeDeadline_NotRequested()
            {
                var service = new PushSyncService(MockPushSyncStore(new[] { new SyncAction() {
                    Id = new LegacyLocationPushSyncRequest(Now).Id,
                    Deadline = Now.AddMinutes(15)
                },new SyncAction() {
                    Id = new DevicePushSyncRequest("deviceId", Now).Id,
                    Deadline = Now.AddMinutes(30)
                } }).Object, MockPushServiceClient(), MockLogger());
                var res = await service.RequestSync(userId, new DevicePushSyncRequest("deviceId", Now.AddMinutes(25)), Now);
                Assert.False(res.SyncRequested);
                Assert.Equal(Now.AddMinutes(15), res.SyncPendingFor);
            }

            private class TestPushSyncRequest : SyncRequestBase, ISyncRequest
            {
                public TestPushSyncRequest(DateTimeOffset deadline, TimeSpan allowMissed) : base(deadline)
                {
                    AllowMissed = allowMissed;
                }

                public new TimeSpan AllowMissed { get; set; }

                public string Id => "test";

                public Dictionary<string, string> GetChannelOptions()
                {
                    return new Dictionary<string, string>();
                }

                public string GetPayload()
                {
                    return null;
                }
            }

            [Fact]
            public async void RequestMissed_RequestAgain()
            {
                var missedDeadline = Now.AddMinutes(-10);
                var service = new PushSyncService(MockPushSyncStore(new[] { new SyncAction() {
                    Id = "test",
                    Deadline = missedDeadline
                }}).Object, MockPushServiceClient(), MockLogger());

                var res = await service.RequestSync(userId, new TestPushSyncRequest(Now, new TimeSpan(0, 5, 0)), Now);
                Assert.True(res.SyncRequested);
                Assert.Null(res.SyncPendingFor);
            }

            [Fact]
            public async void RequestPending_NotRequested()
            {
                var missedDeadline = Now.AddMinutes(-10);
                var service = new PushSyncService(MockPushSyncStore(new[] { new SyncAction() {
                    Id = "test",
                    Deadline = missedDeadline
                }}).Object, MockPushServiceClient(), MockLogger());

                var res = await service.RequestSync(userId, new TestPushSyncRequest(Now, new TimeSpan(0, 11, 0)), Now);
                Assert.False(res.SyncRequested);
                Assert.Equal(missedDeadline, res.SyncPendingFor);
            }
        }

        public class SetRequestedExternal
        {
            [Fact]
            public async void RequestedLater_Add()
            {
                var mockPushSyncStore = MockPushSyncStore(new[] {new SyncAction() {
                    Id = new LegacyLocationPushSyncRequest(Now).Id,
                    Deadline = Now.AddMinutes(30)
                } });
                var service = new PushSyncService(mockPushSyncStore.Object, MockPushServiceClient(), MockLogger());
                var syncRequest = new LegacyLocationPushSyncRequest(Now.AddMinutes(25));
                await service.SetRequestedExternal(userId, syncRequest);
                mockPushSyncStore.Verify(v => v.AddSyncAction(userId, syncRequest.Id, Now.AddMinutes(25)), Times.Once);
            }

            [Fact]
            public async void None_Add()
            {
                var mockPushSyncStore = MockPushSyncStore(new[] {new SyncAction() {
                    Id = new DevicePushSyncRequest("deviceId", Now).Id,
                    Deadline = Now.AddMinutes(10)
                } });
                var service = new PushSyncService(mockPushSyncStore.Object, MockPushServiceClient(), MockLogger());
                var syncRequest = new LegacyLocationPushSyncRequest(Now.AddMinutes(25));
                await service.SetRequestedExternal(userId, syncRequest);
                mockPushSyncStore.Verify(v => v.AddSyncAction(userId, syncRequest.Id, Now.AddMinutes(25)), Times.Once);
            }

            [Fact]
            public async void Earlier_NotAdded()
            {
                var mockPushSyncStore = MockPushSyncStore(new[] {new SyncAction() {
                    Id = new LegacyLocationPushSyncRequest(Now.AddMinutes(10)).Id,
                    Deadline = Now.AddMinutes(10)
                } });
                var service = new PushSyncService(mockPushSyncStore.Object, MockPushServiceClient(), MockLogger());
                await service.SetRequestedExternal(userId, new LegacyLocationPushSyncRequest(Now.AddMinutes(25)));
                mockPushSyncStore.Verify(v => v.AddSyncAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
            }
        }
    }
}

