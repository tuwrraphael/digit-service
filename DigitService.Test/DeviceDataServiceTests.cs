using CalendarService.Client;
using CalendarService.Models;
using Digit.DeviceSynchronization.Impl;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitPushService.Client;
using Moq;
using System;
using System.Threading.Tasks;
using TravelService.Client;
using TravelService.Models.Directions;
using Xunit;

namespace DigitService.Test
{
    public class DeviceDataServiceTests
    {
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
                deviceSyncStoreMock.Setup(d => d.DeviceClaimedByAsync(deviceId)).Returns(Task.FromResult(userId));
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                focusStoreMock.Setup(v => v.GetActiveAsync(userId)).Returns(Task.FromResult(new[] {
                        new FocusItem()
                        {
                            CalendarEventFeedId =feedId,
                            CalendarEventId = "evt1",
                            IndicateTime = now.AddMinutes(30)
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
                var service = new DeviceDataService(focusStoreMock.Object,
                deviceSyncStoreMock.Object, calendarServiceMock.Object, travelServiceMock.Object);
                var data = await service.GetDeviceData(userId, deviceId);
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
                deviceSyncStoreMock.Setup(d => d.DeviceClaimedByAsync(deviceId)).Returns(Task.FromResult(userId));
                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var calendarServiceMock = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
                var travelServiceMock = new Mock<ITravelServiceClient>(MockBehavior.Strict);
                var focusStoreMock = new Mock<IFocusStore>(MockBehavior.Strict);
                focusStoreMock.Setup(v => v.GetActiveAsync(userId)).Returns(Task.FromResult(new[] {
                        new FocusItem()
                        {
                            CalendarEventFeedId =feedId,
                            Directions = new DirectionsMetadata()
                            {
                                Key = directionsKey
                            },
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
                var service = new DeviceDataService(focusStoreMock.Object,
                    deviceSyncStoreMock.Object,
                    calendarServiceMock.Object,
                    travelServiceMock.Object);
                var data = await service.GetDeviceData(userId, deviceId);
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
                Assert.Equal("Arrival3", data.Directions.Legs[1].ArrivalStop);
            }
        }
    }
}

