using CalendarService.Models;
using DigitPushService.Client;
using DigitService.Controllers;
using DigitService.Models;
using DigitService.Service;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelService.Models.Directions;
using Xunit;

namespace DigitService.Test
{
    public class LocationServiceTests
    {
        const string userId = "12345";

        public class RequestLocation
        {
            [Fact]
            public async void RequestLocation_NoneStored()
            {
                var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStore.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult((Location)null));
                locationStore.Setup(v => v.GetLocationRequestTimeAsync(userId)).Returns(Task.FromResult((DateTimeOffset?)null));
                var digitPushServiceClient = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var logger = Mock.Of<IDigitLogger>();
                var locationService = new LocationService(digitPushServiceClient.Object, locationStore.Object, logger);
                var res = await locationService.RequestLocationAsync(userId, new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero), null);
                Assert.True(res.LocationRequestSent);
                Assert.Equal(new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero), res.LocationRequestTime);
            }

            [Fact]
            public async void RequestLocation_StoredOutdated()
            {
                var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
                var stored = new Location()
                {
                    Latitude = 40,
                    Longitude = 41,
                    Timestamp = new DateTimeOffset(2018, 01, 01, 0, 10, 0, TimeSpan.Zero)
                };
                locationStore.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult(stored));
                locationStore.Setup(v => v.GetLocationRequestTimeAsync(userId)).Returns(Task.FromResult((DateTimeOffset?)new DateTimeOffset(2018, 01, 01, 0, 5, 0, TimeSpan.Zero)));
                var digitPushServiceClient = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var logger = Mock.Of<IDigitLogger>();
                var locationService = new LocationService(digitPushServiceClient.Object, locationStore.Object, logger);
                var res = await locationService.RequestLocationAsync(userId, new DateTimeOffset(2018, 01, 01, 1, 0, 0, TimeSpan.Zero), null);
                Assert.True(res.LocationRequestSent);
                Assert.Equal(new DateTimeOffset(2018, 01, 01, 1, 0, 0, TimeSpan.Zero), res.LocationRequestTime);
            }

            [Fact]
            public async void RequestLocation_StoredValid()
            {
                var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
                var stored = new Location()
                {
                    Latitude = 40,
                    Longitude = 41,
                    Timestamp = new DateTimeOffset(2018, 01, 01, 0, 59, 0, TimeSpan.Zero)
                };
                locationStore.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult(stored));
                locationStore.Setup(v => v.GetLocationRequestTimeAsync(userId)).Returns(Task.FromResult((DateTimeOffset?)new DateTimeOffset(2018, 01, 01, 0, 58, 0, TimeSpan.Zero)));
                var digitPushServiceClient = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var logger = Mock.Of<IDigitLogger>();
                var locationService = new LocationService(digitPushServiceClient.Object, locationStore.Object, logger);
                var res = await locationService.RequestLocationAsync(userId, new DateTimeOffset(2018, 01, 01, 1, 0, 0, TimeSpan.Zero), null);
                Assert.False(res.LocationRequestSent);
                Assert.Null(res.LocationRequestTime);
            }

            [Fact]
            public async void RequestLocation_RequestPending()
            {
                var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
                var stored = new Location()
                {
                    Latitude = 40,
                    Longitude = 41,
                    Timestamp = new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero)
                };
                locationStore.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult(stored));
                locationStore.Setup(v => v.GetLocationRequestTimeAsync(userId)).Returns(Task.FromResult((DateTimeOffset?)new DateTimeOffset(2018, 01, 01, 0, 45, 0, TimeSpan.Zero)));
                var digitPushServiceClient = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                var logger = Mock.Of<IDigitLogger>();
                var locationService = new LocationService(digitPushServiceClient.Object, locationStore.Object, logger);
                var res = await locationService.RequestLocationAsync(userId, new DateTimeOffset(2018, 01, 01, 1, 0, 0, TimeSpan.Zero), null);
                Assert.False(res.LocationRequestSent);
                Assert.Equal(new DateTimeOffset(2018, 01, 01, 0, 45, 0, TimeSpan.Zero), res.LocationRequestTime);
            }
        }

        public class LocationUpdateReceived
        {
            private readonly ILocationStore locationStore;

            private readonly DateTimeOffset now = new DateTimeOffset(2018, 1, 1, 10, 0, 0, TimeSpan.Zero);
            private readonly IDigitLogger logger;
            private readonly IDigitPushServiceClient digitPushServiceClient;

            public LocationUpdateReceived()
            {
                var locationStoreMock = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStoreMock.Setup(v => v.UpdateLocationAsync(userId, It.IsAny<Location>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.SetLocationRequestedForAsync(userId, It.IsAny<DateTimeOffset>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.SetGeofenceRequestedAsync(userId, It.IsAny<GeofenceRequest>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.IsGeofenceActiveAsync(userId, It.IsAny<GeofenceRequest>())).Returns(Task.FromResult(false));
                locationStoreMock.Setup(v => v.ClearGeofenceAsync(userId)).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult((Location)null));
                locationStore = locationStoreMock.Object;

                var digitPushServiceClientMock = new Mock<IDigitPushServiceClient>(MockBehavior.Strict);
                logger = Mock.Of<IDigitLogger>();
                digitPushServiceClient = digitPushServiceClientMock.Object;
            }

            private FocusDeparture GetDeparture(DateTimeOffset departureTime, DateTimeOffset firstStopTime, Event evt = null)
            {
                return new FocusDeparture()
                {
                    DepartureTime = departureTime,
                    Route = new Route()
                    {
                        DepatureTime = departureTime,
                        Steps = new[]{ new Step() {
                            DepartureTime = firstStopTime
                        } }
                    },
                    Event = evt
                };
            }

            [Fact]
            public async void LocationUpdateReceived_NoDepartures_NoUpdateRequired()
            {
                var locationService = new LocationService(digitPushServiceClient, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {

                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                });
                Assert.Null(response.NextUpdateRequiredAt);
            }

            [Fact]
            public async void LocationUpdateReceived_UpcomingDeparture_LocationUpdateRequired()
            {
                var locationService = new LocationService(digitPushServiceClient, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                {
                    GetDeparture(now.AddMinutes(30), now.AddMinutes(45))
                }
                });
                Assert.Equal(now.AddMinutes(-2).AddMinutes(16), response.NextUpdateRequiredAt);
            }

            [Fact]
            public async void LocationUpdateReceived_PendingDeparture_UpdateForFirstStep()
            {
                var locationService = new LocationService(digitPushServiceClient, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                {
                    GetDeparture(now.AddMinutes(9), now.AddMinutes(24), new Event() {
                        End = now.AddMinutes(180)
                    })
                }
                });
                Assert.Equal(now.AddMinutes(16).AddSeconds(30), response.NextUpdateRequiredAt);
            }

            [Fact]
            public async void LocationUpdateReceived_UpcomingDepartures_EarlierUpdate()
            {
                var locationService = new LocationService(digitPushServiceClient, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                {
                    GetDeparture(now.AddMinutes(20), now.AddMinutes(35)),
                    GetDeparture(now.AddMinutes(45), now.AddMinutes(60)),
                }
                });
                Assert.Equal(now.AddMinutes(9), response.NextUpdateRequiredAt);
            }

            [Fact]
            public async void LocationUpdateReceived_PastDeparture_NoUpdate()
            {
                var locationService = new LocationService(digitPushServiceClient, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                {
                       GetDeparture(now.AddMinutes(-2), now.AddMinutes(10)),
                }
                });
                Assert.Null(response.NextUpdateRequiredAt);
            }

            [Fact]
            public async void LocationUpdateReceived_UpcomingDeparture_NoGeofenceRequested()
            {
                var locationService = new LocationService(digitPushServiceClient, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                {
                    GetDeparture(now.AddMinutes(30), now.AddMinutes(45))
                }
                });
                Assert.Null(response.RequestGeofence);
            }

            [Fact]
            public async void LocationUpdateReceived_PendingDeparture_GeofenceRequested()
            {
                var locationService = new LocationService(digitPushServiceClient, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                {
                    GetDeparture(now.AddMinutes(9), now.AddMinutes(24), new Event() {
                        End = now.AddMinutes(60)
                    })
                }
                });
                Assert.NotNull(response.RequestGeofence);
                Assert.Equal(now, response.RequestGeofence.Start);
                Assert.Equal(now.AddMinutes(60), response.RequestGeofence.End);
            }

            [Fact]
            public async void LocationUpdateReceived_PendingDeparture_GeofenceActive()
            {
                var locationStoreMock = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStoreMock.Setup(v => v.UpdateLocationAsync(userId, It.IsAny<Location>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.SetLocationRequestedForAsync(userId, It.IsAny<DateTimeOffset>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.SetGeofenceRequestedAsync(userId, It.IsAny<GeofenceRequest>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.IsGeofenceActiveAsync(userId, It.Is<GeofenceRequest>(d => d.Start == now && d.End == now.AddMinutes(60)))).Returns(Task.FromResult(true));
                locationStoreMock.Setup(v => v.IsGeofenceActiveAsync(userId, It.Is<GeofenceRequest>(d => d.Start == now && d.End == now))).Returns(Task.FromResult(true));
                locationStoreMock.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult((Location)null));
                var locationService = new LocationService(digitPushServiceClient, locationStoreMock.Object, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                {
                    GetDeparture(now.AddMinutes(9), now.AddMinutes(24), new Event() {
                        End = now.AddMinutes(60)
                    })
                }
                });
                Assert.Null(response.RequestGeofence);
            }

            [Fact]
            public async void LocationUpdateReceived_Geofence_Cleared()
            {
                var locationStoreMock = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStoreMock.Setup(v => v.UpdateLocationAsync(userId, It.IsAny<Location>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.SetLocationRequestedForAsync(userId, It.IsAny<DateTimeOffset>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.SetGeofenceRequestedAsync(userId, It.IsAny<GeofenceRequest>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.IsGeofenceActiveAsync(userId, It.Is<GeofenceRequest>(d => d.Start == now && d.End == now))).Returns(Task.FromResult(true));
                locationStoreMock.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult(new Location()
                {
                    Latitude = 48.204048,
                    Longitude = 16.376781,
                    Timestamp = now.AddMinutes(-10)
                }));
                locationStoreMock.Setup(v => v.ClearGeofenceAsync(userId)).Returns(Task.CompletedTask).Verifiable();
                var locationService = new LocationService(digitPushServiceClient, locationStoreMock.Object, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Latitude = 48.204598,
                    Longitude = 16.375606,
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                    {
                    }
                });
                locationStoreMock.Verify(v => v.ClearGeofenceAsync(userId), Times.Once);
            }

            [Fact]
            public async void LocationUpdateReceived_Geofence_RemainsActive()
            {
                var locationStoreMock = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStoreMock.Setup(v => v.UpdateLocationAsync(userId, It.IsAny<Location>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.SetLocationRequestedForAsync(userId, It.IsAny<DateTimeOffset>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.SetGeofenceRequestedAsync(userId, It.IsAny<GeofenceRequest>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.IsGeofenceActiveAsync(userId, It.Is<GeofenceRequest>(d => d.Start == now && d.End == now))).Returns(Task.FromResult(true));
                locationStoreMock.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult(new Location()
                {
                    Latitude = 48.204048,
                    Longitude = 16.376781,
                    Timestamp = now.AddMinutes(-10)
                }));
                locationStoreMock.Setup(v => v.ClearGeofenceAsync(userId)).Returns(Task.CompletedTask).Verifiable();
                var locationService = new LocationService(digitPushServiceClient, locationStoreMock.Object, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Latitude = 48.204083,
                    Longitude = 16.376732,
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    Departures = new List<FocusDeparture>()
                    {
                    }
                });
                locationStoreMock.Verify(v => v.ClearGeofenceAsync(userId), Times.Never);
            }
        }
    }
}