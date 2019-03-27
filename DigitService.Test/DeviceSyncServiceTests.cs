using CalendarService.Client;
using Digit.DeviceSynchronization.Impl;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Service;
using DigitPushService.Client;
using Moq;
using PushServer.PushConfiguration.Abstractions.Models;
using System.Threading.Tasks;
using TravelService.Client;
using Xunit;

namespace DigitService.Test
{
    public class DeviceSyncServiceTests
    {
        const string userId = "12345";

        public class RequestSynchronizationAsync
        {
            [Fact]
            public async void Stores()
            {
                const string userId = "user";
                const string channel1Id = "channel1";
                const string deviceId = "devId";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                deviceSyncStoreMock.Setup(v => v.CreateAsync(userId, deviceId, It.IsAny<DeviceSyncRequest>()))
                    .Returns(Task.CompletedTask);
                deviceSyncStoreMock.Setup(v => v.DeviceClaimedByAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(null));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId].GetAllAsync()).Returns(Task.FromResult(new[] {
                    new PushChannelConfiguration()
                    {
                        Id = channel1Id,
                        Options = new PushChannelOptions()
                    }
                }));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId][channel1Id].Options.PutAsync(It.IsAny<PushChannelOptions>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                var deviceSync = new DeviceSyncService(digitPushServiceClientMock.Object, deviceSyncStoreMock.Object);

                await deviceSync.RequestSynchronizationAsync(userId, deviceId, new DeviceSyncRequest()
                {
                    PushChannelId = channel1Id
                });
                digitPushServiceClientMock.Verify(v => v.PushChannels[userId][channel1Id].Options.PutAsync(It.Is<PushChannelOptions>(d => d.ContainsKey($"digit.sync.{deviceId}"))), Times.Once);
                deviceSyncStoreMock.Verify(v => v.CreateAsync(userId, deviceId, It.IsAny<DeviceSyncRequest>()));
            }

            [Fact]
            public async void RequestedTwice_NoUpdate()
            {
                const string userId = "user";
                const string channel1Id = "channel1";
                const string channel2Id = "channel2";
                const string deviceId = "devId";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                deviceSyncStoreMock.Setup(v => v.CreateAsync(userId, deviceId, It.IsAny<DeviceSyncRequest>()))
                    .Returns(Task.CompletedTask);
                deviceSyncStoreMock.Setup(v => v.DeviceClaimedByAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(userId));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId].GetAllAsync()).Returns(Task.FromResult(new[] {
                    new PushChannelConfiguration()
                    {
                        Id = channel1Id,
                        Options = new PushChannelOptions()
                        {
                            { $"digit.sync.{deviceId}",null}
                        }
                    },
                    new PushChannelConfiguration()
                    {
                        Id = channel2Id,
                        Options = new PushChannelOptions()
                    }
                }));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId][channel1Id].Options.PutAsync(It.IsAny<PushChannelOptions>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId][channel2Id].Options.PutAsync(It.IsAny<PushChannelOptions>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();

                var deviceSync = new DeviceSyncService(digitPushServiceClientMock.Object, deviceSyncStoreMock.Object);
                await deviceSync.RequestSynchronizationAsync(userId, deviceId, new DeviceSyncRequest()
                {
                    PushChannelId = channel1Id
                });
                digitPushServiceClientMock.Verify(v => v.PushChannels[userId][It.IsAny<string>()].Options.PutAsync(It.IsAny<PushChannelOptions>()), Times.Never);
            }


            [Fact]
            public async void RequestedTwice_UpdatesChannel()
            {
                const string userId = "user";
                const string channel1Id = "channel1";
                const string channel2Id = "channel2";
                const string deviceId = "devId";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                deviceSyncStoreMock.Setup(v => v.CreateAsync(userId, deviceId, It.IsAny<DeviceSyncRequest>()))
                    .Returns(Task.CompletedTask);
                deviceSyncStoreMock.Setup(v => v.DeviceClaimedByAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(userId));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId].GetAllAsync()).Returns(Task.FromResult(new[] {
                    new PushChannelConfiguration()
                    {
                        Id = channel1Id,
                        Options = new PushChannelOptions()
                        {
                            { $"digit.sync.{deviceId}", null },
                            { "location", null}
                        }
                    },
                    new PushChannelConfiguration()
                    {
                        Id = channel2Id,
                        Options = new PushChannelOptions() {
                            { "location2", "test"}
                        }
                    }
                }));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId][channel1Id].Options.PutAsync(It.IsAny<PushChannelOptions>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId][channel2Id].Options.PutAsync(It.IsAny<PushChannelOptions>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
                var deviceSync = new DeviceSyncService(digitPushServiceClientMock.Object, deviceSyncStoreMock.Object);
                await deviceSync.RequestSynchronizationAsync(userId, deviceId, new DeviceSyncRequest()
                {
                    PushChannelId = channel2Id
                });
                digitPushServiceClientMock.Verify(v => v.PushChannels[userId][channel1Id].Options.PutAsync(
                    It.Is<PushChannelOptions>(d => d.Count == 1 && d.ContainsKey("location"))
                    ), Times.Once);
                digitPushServiceClientMock.Verify(v => v.PushChannels[userId][channel2Id].Options.PutAsync(
                    It.Is<PushChannelOptions>(d => d.Count == 2 && d["location2"] == "test" && d.ContainsKey($"digit.sync.{deviceId}"))
                    ), Times.Once);
            }

            [Fact]
            public async void StoredForOtherUser_Exeception()
            {
                const string userId = "user";
                const string deviceId = "devId";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                deviceSyncStoreMock.Setup(v => v.CreateAsync(userId, deviceId, It.IsAny<DeviceSyncRequest>()))
                    .Returns(Task.CompletedTask);
                deviceSyncStoreMock.Setup(v => v.DeviceClaimedByAsync(It.IsAny<string>())).Returns(Task.FromResult("user2"));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId].GetAllAsync())
                    .Returns(Task.FromResult(new[] { new PushChannelConfiguration() {
                        Id = "channel"
                    } }));
                var deviceSync = new DeviceSyncService(digitPushServiceClientMock.Object, deviceSyncStoreMock.Object);
                var ex = await Assert.ThrowsAsync<DeviceClaimedException>(async () => await deviceSync.RequestSynchronizationAsync(userId, deviceId, new DeviceSyncRequest()
                {
                    PushChannelId = "channel"
                }));
            }

            [Fact]
            public async void ChannelNotFound_Exception()
            {
                const string userId = "user";
                const string deviceId = "devId";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                deviceSyncStoreMock.Setup(v => v.DeviceClaimedByAsync(deviceId)).Returns(Task.FromResult<string>(null));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId].GetAllAsync())
                    .Returns(Task.FromResult(new PushChannelConfiguration[0]));
                var deviceSync = new DeviceSyncService(digitPushServiceClientMock.Object, deviceSyncStoreMock.Object);
                var ex = await Assert.ThrowsAsync<PushChannelNotFoundException>(async () => await deviceSync.RequestSynchronizationAsync(userId, deviceId, new DeviceSyncRequest()
                {
                    PushChannelId = "channel"
                }));
            }
        }
    }
}

