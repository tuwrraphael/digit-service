using Digit.Abstractions.Service;
using Digit.DeviceSynchronization;
using Digit.DeviceSynchronization.Impl;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Service;
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

        private static IDigitPushServiceClient MockDigitPushServiceCLient()
        {
            var digitPushServiceMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
            digitPushServiceMock.Setup(v => v[It.IsAny<string>()].DigitSync.Location(It.IsAny<LocationSyncRequest>()))
                .Returns(Task.CompletedTask);
            return digitPushServiceMock.Object;
        }

        public class RequestSync
        {
            [Fact]
            public async void NoPending_Requested()
            {
                var service = new PushSyncService(MockPushSyncStore(new SyncAction[] { }).Object, MockDigitPushServiceCLient(),
                    Mock.Of<IFocusStore>());
                var res = await service.RequestLocationSync(userId, Now, Now);
                Assert.True(res.SyncRequested);
                Assert.False(res.SyncPendingFor.HasValue);
            }

            [Fact]
            public async void PendingAfterDeadline_Requested()
            {
                var service = new PushSyncService(MockPushSyncStore(new[] { new SyncAction() {
                    Id = "locationSync",
                    Deadline = Now.AddMinutes(30)
                } }).Object, MockDigitPushServiceCLient(),
                    Mock.Of<IFocusStore>());
                var res = await service.RequestLocationSync(userId, Now, Now.AddMinutes(15));
                Assert.True(res.SyncRequested);
                Assert.Equal(Now.AddMinutes(30), res.SyncPendingFor);
            }

            [Fact]
            public async void PendingBeforeDeadline_NotRequested()
            {
                var service = new PushSyncService(MockPushSyncStore(new[] { new SyncAction() {
                    Id = "locationSync",
                    Deadline = Now.AddMinutes(15)
                } }).Object, MockDigitPushServiceCLient(),
                    Mock.Of<IFocusStore>());
                var res = await service.RequestLocationSync(userId, Now, Now.AddMinutes(30));
                Assert.False(res.SyncRequested);
                Assert.Equal(Now.AddMinutes(15), res.SyncPendingFor);
            }

            [Fact]
            public async void RequestMissed_RequestAgain()
            {
                var missedDeadline = Now.Add(-DeviceSyncConstants.PushMissed);
                var service = new PushSyncService(MockPushSyncStore(new[] { new SyncAction() {
                    Id = "test",
                    Deadline = missedDeadline
                }}).Object, MockDigitPushServiceCLient(),
                    Mock.Of<IFocusStore>());

                var res = await service.RequestLocationSync(userId, Now.AddSeconds(1), Now.AddSeconds(1));
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
                }}).Object, MockDigitPushServiceCLient(),
                    Mock.Of<IFocusStore>());

                var res = await service.RequestLocationSync(userId, Now, Now);
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
                    Id ="locationSync",
                    Deadline = Now.AddMinutes(30)
                } });
                var service = new PushSyncService(mockPushSyncStore.Object, MockDigitPushServiceCLient(),
                    Mock.Of<IFocusStore>());
                await service.SetLocationRequestedExternal(userId, Now.AddMinutes(25));
                mockPushSyncStore.Verify(v => v.AddSyncAction(userId, "locationSync", Now.AddMinutes(25)), Times.Once);
            }

            [Fact]
            public async void None_Add()
            {
                var mockPushSyncStore = MockPushSyncStore(new[] {new SyncAction() {
                    Id = "other",
                    Deadline = Now.AddMinutes(10)
                } });
                var service = new PushSyncService(mockPushSyncStore.Object, MockDigitPushServiceCLient(),
                    Mock.Of<IFocusStore>());
                await service.SetLocationRequestedExternal(userId, Now.AddMinutes(25));
                mockPushSyncStore.Verify(v => v.AddSyncAction(userId, "locationSync", Now.AddMinutes(25)), Times.Once);
            }

            [Fact]
            public async void Earlier_NotAdded()
            {
                var mockPushSyncStore = MockPushSyncStore(new[] {new SyncAction() {
                    Id = "locationSync",
                    Deadline = Now.AddMinutes(10)
                } });
                var service = new PushSyncService(mockPushSyncStore.Object, MockDigitPushServiceCLient(),
                    Mock.Of<IFocusStore>());
                await service.SetLocationRequestedExternal(userId, Now.AddMinutes(25));
                mockPushSyncStore.Verify(v => v.AddSyncAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
            }
        }
    }
}

