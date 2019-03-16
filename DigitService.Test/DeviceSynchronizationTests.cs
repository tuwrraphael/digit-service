using CalendarService.Client;
using CalendarService.Models;
using Digit.DeviceSynchronization.Impl;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitPushService.Client;
using DigitPushService.Models;
using Moq;
using PushServer.PushConfiguration.Abstractions.Models;
using System;
using System.Threading.Tasks;
using TravelService.Client;
using TravelService.Models.Directions;
using Xunit;

namespace DigitService.Test
{
    public class DeviceSynchronizationTests
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
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
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
                var focusItemsDigest = new Mock<IFocusItemDigest>(MockBehavior.Strict);

                var deviceSync = new DeviceSynchronization(focusStoreMock.Object, digitPushServiceClientMock.Object, deviceSyncStoreMock.Object, focusItemsDigest.Object,
                    calendarServiceMock.Object, travelServiceMock.Object);

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
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
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
                var focusItemsDigest = new Mock<IFocusItemDigest>(MockBehavior.Strict);

                var deviceSync = new DeviceSynchronization(focusStoreMock.Object, digitPushServiceClientMock.Object, deviceSyncStoreMock.Object, focusItemsDigest.Object,
                    calendarServiceMock.Object, travelServiceMock.Object);
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
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
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
                var deviceSync = new DeviceSynchronization(focusStoreMock.Object, digitPushServiceClientMock.Object,
                    deviceSyncStoreMock.Object, focusItemsDigestMock.Object,
                    calendarServiceMock.Object, travelServiceMock.Object);
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
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
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
                var deviceSync = new DeviceSynchronization(focusStoreMock.Object, digitPushServiceClientMock.Object, deviceSyncStoreMock.Object,
                    focusItemsDigestMock.Object,
                    calendarServiceMock.Object,
                    travelServiceMock.Object);
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
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                deviceSyncStoreMock.Setup(v => v.DeviceClaimedByAsync(deviceId)).Returns(Task.FromResult<string>(null));
                digitPushServiceClientMock.Setup(v => v.PushChannels[userId].GetAllAsync())
                    .Returns(Task.FromResult(new PushChannelConfiguration[0]));
                var deviceSync = new DeviceSynchronization(focusStoreMock.Object, digitPushServiceClientMock.Object, deviceSyncStoreMock.Object,
                    focusItemsDigestMock.Object,
                    calendarServiceMock.Object,
                    travelServiceMock.Object);
                var ex = await Assert.ThrowsAsync<PushChannelNotFoundException>(async () => await deviceSync.RequestSynchronizationAsync(userId, deviceId, new DeviceSyncRequest()
                {
                    PushChannelId = "channel"
                }));
            }
        }

        public class TriggerSynchronizationAsync
        {
            [Fact]
            public async void DigestChanged_PushExecuted()
            {
                const string userId = "user";
                const string itemId = "item";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                digitPushServiceClientMock.Setup(v => v.Push[userId].Create(It.IsAny<PushRequest>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
                focusItemsDigestMock.Setup(v => v.GetDigestAsync(It.IsAny<FocusItem>()))
                    .Returns(Task.FromResult("digest3"));
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                focusStoreMock.Setup(v => v.GetActiveAsync(userId))
                .Returns(Task.FromResult(new[] { new FocusItem() {
                    Id = itemId
                    } }));
                deviceSyncStoreMock.Setup(v => v.GetForUserAsync(userId))
                    .Returns(Task.FromResult(new[]  { new Device() {
                        Id = "device1",
                        FocusItemDigest = "digest1",
                        FocusItemId = itemId
                    },new Device() {
                        Id = "device2",
                        FocusItemDigest = "digest2",
                        FocusItemId = itemId
                    } ,
                    new Device()
                    {
                        Id = "device3",
                        FocusItemDigest = "digest3",
                        FocusItemId = itemId
                    } }));

                var service = new DeviceSynchronization(focusStoreMock.Object,
                    digitPushServiceClientMock.Object,
                    deviceSyncStoreMock.Object, focusItemsDigestMock.Object,
                    calendarServiceMock.Object, travelServiceMock.Object);
                await service.TriggerSynchronizationAsync(userId);
                digitPushServiceClientMock.Verify(v => v.Push[userId].Create(It.Is<PushRequest>(d => d.ChannelOptions.ContainsKey($"digit.sync.device1"))), Times.Once);
                digitPushServiceClientMock.Verify(v => v.Push[userId].Create(It.Is<PushRequest>(d => d.ChannelOptions.ContainsKey($"digit.sync.device2"))), Times.Once);
            }

            [Fact]
            public async void FirstItem_Push()
            {
                const string userId = "user";
                const string itemId = "item";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                digitPushServiceClientMock.Setup(v => v.Push[userId].Create(It.IsAny<PushRequest>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
                focusItemsDigestMock.Setup(v => v.GetDigestAsync(It.IsAny<FocusItem>()))
                    .Returns(Task.FromResult("digest3"));
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                focusStoreMock.Setup(v => v.GetActiveAsync(userId))
                .Returns(Task.FromResult(new[] { new FocusItem() {
                    Id = itemId
                    } }));
                deviceSyncStoreMock.Setup(v => v.GetForUserAsync(userId))
                    .Returns(Task.FromResult(new[]  { new Device() {
                        Id = "device1",
                        FocusItemDigest = null,
                        FocusItemId = null
                    }}));

                var service = new DeviceSynchronization(focusStoreMock.Object,
                    digitPushServiceClientMock.Object,
                    deviceSyncStoreMock.Object, focusItemsDigestMock.Object,
                    calendarServiceMock.Object, travelServiceMock.Object);
                await service.TriggerSynchronizationAsync(userId);
                digitPushServiceClientMock.Verify(v => v.Push[userId].Create(It.Is<PushRequest>(d => d.ChannelOptions.ContainsKey($"digit.sync.device1"))), Times.Once);
            }

            [Fact]
            public async void ItemRemoved_Push()
            {
                const string userId = "user";
                const string itemId = "item";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                digitPushServiceClientMock.Setup(v => v.Push[userId].Create(It.IsAny<PushRequest>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
                focusItemsDigestMock.Setup(v => v.GetDigestAsync(It.IsAny<FocusItem>()))
                    .Returns(Task.FromResult("digest3"));
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                focusStoreMock.Setup(v => v.GetActiveAsync(userId))
                .Returns(Task.FromResult(new FocusItem[0]));
                deviceSyncStoreMock.Setup(v => v.GetForUserAsync(userId))
                    .Returns(Task.FromResult(new[]  { new Device() {
                        Id = "device1",
                        FocusItemDigest = "digest",
                        FocusItemId = itemId
                    }}));

                var service = new DeviceSynchronization(focusStoreMock.Object,
                    digitPushServiceClientMock.Object,
                    deviceSyncStoreMock.Object, focusItemsDigestMock.Object,
                    calendarServiceMock.Object, travelServiceMock.Object);
                await service.TriggerSynchronizationAsync(userId);
                digitPushServiceClientMock.Verify(v => v.Push[userId].Create(It.Is<PushRequest>(d => d.ChannelOptions.ContainsKey($"digit.sync.device1"))), Times.Once);
            }

            [Fact]
            public async void NoChanges_NoPush()
            {
                const string userId = "user";
                const string itemId = "item";
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                digitPushServiceClientMock.Setup(v => v.Push[userId].Create(It.IsAny<PushRequest>()))
                    .Returns(Task.CompletedTask)
                    .Verifiable();
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
                focusItemsDigestMock.Setup(v => v.GetDigestAsync(It.IsAny<FocusItem>()))
                    .Returns(Task.FromResult("digest3"));
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                focusStoreMock.Setup(v => v.GetActiveAsync(userId))
                .Returns(Task.FromResult(new[] { new FocusItem {
                    Id = itemId
                } }));
                deviceSyncStoreMock.Setup(v => v.GetForUserAsync(userId))
                    .Returns(Task.FromResult(new[]  { new Device() {
                        Id = "device1",
                        FocusItemDigest = "digest3",
                        FocusItemId = itemId
                    }}));

                var service = new DeviceSynchronization(focusStoreMock.Object,
                    digitPushServiceClientMock.Object,
                    deviceSyncStoreMock.Object, focusItemsDigestMock.Object,
                    calendarServiceMock.Object, travelServiceMock.Object);
                await service.TriggerSynchronizationAsync(userId);
                digitPushServiceClientMock.Verify(v => v.Push[userId].Create(It.IsAny<PushRequest>()), Times.Never);
            }
        }

        public class GetDeviceDataAsync
        {
            [Fact]
            public async Task ChooseCorrectItem()
            {
                const string userId = "userId";
                const string deviceId = "devId";
                const string feedId = "feed";
                DateTime now = DateTime.Now;
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                focusStoreMock.Setup(v => v.GetActiveAsync(userId)).Returns(Task.FromResult(new[] {
                        new FocusItem()
                        {
                            CalendarEventFeedId =feedId,
                            CalendarEventId = "evt1",
                            IndicateTime = now.AddMinutes(15)
                        },
                        new FocusItem()
                        {
                            CalendarEventFeedId =feedId,
                            CalendarEventId = "evt2",
                            IndicateTime = now.AddMinutes(-5)
                        },
                        new FocusItem()
                        {
                            CalendarEventFeedId =feedId,
                            CalendarEventId = "evt3",
                            IndicateTime = now.AddMinutes(-10)
                        }
                    }));
                calendarServiceMock.Setup(v => v.Users[userId].Feeds[feedId].Events.Get(It.IsAny<string>()))
                    .Returns<string>(d => Task.FromResult(new Event()
                    {
                        Subject = d + "Subject"
                    }));
                var service = new DeviceSynchronization(focusStoreMock.Object,
                digitPushServiceClientMock.Object,
                deviceSyncStoreMock.Object, focusItemsDigestMock.Object, calendarServiceMock.Object, travelServiceMock.Object);
                var data = await service.GetDeviceDataAsync(userId, deviceId);
                Assert.Equal("evt2Subject", data.Event.Subject);
            }

            [Fact]
            public async Task MapCorrectly()
            {
                const string userId = "userId";
                const string deviceId = "devId";
                const string feedId = "feed";
                const string evtId = "evt";
                const string directionsKey = "dirs";
                DateTime now = DateTime.Now;
                var deviceSyncStoreMock = new Mock<IDeviceSyncStore>(MockBehavior.Strict);
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var focusItemsDigestMock = new Mock<IFocusItemDigest>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                focusStoreMock.Setup(v => v.GetActiveAsync(userId)).Returns(Task.FromResult(new[] {
                        new FocusItem()
                        {
                            CalendarEventFeedId =feedId,
                            DirectionsKey = directionsKey,
                            CalendarEventId = evtId,
                            IndicateTime = now.AddMinutes(-5)
                        }
                    }));
                calendarServiceMock.Setup(v => v.Users[userId].Feeds[feedId].Events.Get(evtId))
                    .Returns(Task.FromResult(new Event()
                    {
                        Subject = "subject",
                        Start = now.AddMinutes(35)
                    }));
                travelServiceMock.Setup(v => v.Directions[directionsKey].GetAsync())
                    .Returns(Task.FromResult(new DirectionsResult()
                    {
                        TransitDirections = new TransitDirections()
                        {
                            Routes = new[] {
                                new Route()
                                {
                                    DepatureTime = now.AddMinutes(2),
                                    ArrivalTime = now.AddMinutes(32),
                                    Steps = new []{
                                        new Step()
                                        {
                                            DepartureStop = new Stop() {Name =   "Departure1" },
                                            ArrivalStop =  new Stop() {Name ="Arrival1" },
                                            DepartureTime = now.AddMinutes(3),
                                            Line = new Line() {
                                                ShortName = "Line1"
                                            },
                                            Headsign = "Direction1"
                                        },
                                        new Step()
                                        {
                                            DepartureStop = new Stop() {Name =   "Departure2" },
                                            ArrivalStop =  new Stop() {Name ="Arrival3" },
                                            DepartureTime = now.AddMinutes(16),
                                            Line = new Line() {
                                                ShortName = "Line2"
                                            },
                                            Headsign = "Direction2"
                                        }
                                    }
                                }
                            }
                        }
                    }));
                var service = new DeviceSynchronization(focusStoreMock.Object,
                    digitPushServiceClientMock.Object,
                    deviceSyncStoreMock.Object,
                    focusItemsDigestMock.Object,
                    calendarServiceMock.Object,
                    travelServiceMock.Object);
                var data = await service.GetDeviceDataAsync(userId, deviceId);
                Assert.Equal("subject", data.Event.Subject);
                Assert.Equal(now.AddMinutes(35), data.Event.Start);
                Assert.Equal(now.AddMinutes(2), data.Directions.DepartureTime);
                Assert.Equal(now.AddMinutes(32), data.Directions.ArrivalTime);
                Assert.Equal(2, data.Directions.Legs.Length);

                Assert.Equal(now.AddMinutes(3), data.Directions.Legs[0].DepartureTime);
                Assert.Equal("Line1", data.Directions.Legs[0].Line);
                Assert.Equal("Direction1", data.Directions.Legs[0].Direction);
                Assert.Equal("Departure1", data.Directions.Legs[0].DepartureStop);
                Assert.Equal("Arrival1", data.Directions.Legs[0].ArrivalStop);

                Assert.Equal(now.AddMinutes(16), data.Directions.Legs[1].DepartureTime);
                Assert.Equal("Line2", data.Directions.Legs[1].Line);
                Assert.Equal("Direction2", data.Directions.Legs[1].Direction);
                Assert.Equal("Departure2", data.Directions.Legs[1].DepartureStop);
                Assert.Equal("Arrival2", data.Directions.Legs[1].ArrivalStop);
            }
        }
    }
}

