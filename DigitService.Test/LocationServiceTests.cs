using CalendarService.Models;
using Digit.Abstractions.Service;
using Digit.DeviceSynchronization.Impl;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Digit.Focus.Model;
using Digit.Focus.Models;
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

        private static IPushSyncService IntegratePushSyncService(DateTimeOffset? locationRequestTime)
        {
            var syncActions = null == locationRequestTime ? new SyncAction[0] :
                new[] { new SyncAction() {
                    Id = new LocationPushSyncRequest(DateTimeOffset.Now).Id,
                    Deadline = locationRequestTime
                }  };
            var pushSyncStoreMock = new Mock<IPushSyncStore>(MockBehavior.Strict);
            pushSyncStoreMock.Setup(v => v.GetPendingSyncActions(It.IsAny<string>()))
                .Returns(Task.FromResult(syncActions));
            pushSyncStoreMock.Setup(v => v.AddSyncAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()))
                .Returns(Task.FromResult(syncActions));
            return new PushSyncService(pushSyncStoreMock.Object, Mock.Of<IDebouncedPushService>());
        }

        public class RequestLocation
        {
            [Fact]
            public async void RequestLocation_NoneStored()
            {
                var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStore.Setup(v => v.GetNonExpiredGeofenceRequests(userId, It.IsAny<DateTimeOffset>())).Returns(Task.FromResult(new GeofenceRequest[0]));
                locationStore.Setup(v => v.SetGeofenceRequests(userId, It.IsAny<GeofenceRequest[]>())).Returns(Task.CompletedTask);
                locationStore.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult((Location)null));
                var logger = Mock.Of<IDigitLogger>();
                var locationService = new LocationService(IntegratePushSyncService(null), locationStore.Object, logger);
                var res = await locationService.RequestLocationAsync(userId, new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero), null);
                Assert.True(res.LocationRequestSent);
                Assert.Equal(new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero), res.LocationRequestTime);
            }

            [Fact]
            public async void RequestLocation_StoredOutdated()
            {
                var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStore.Setup(v => v.GetNonExpiredGeofenceRequests(userId, It.IsAny<DateTimeOffset>())).Returns(Task.FromResult(new GeofenceRequest[0]));
                locationStore.Setup(v => v.SetGeofenceRequests(userId, It.IsAny<GeofenceRequest[]>())).Returns(Task.CompletedTask);
                var stored = new Location()
                {
                    Latitude = 40,
                    Longitude = 41,
                    Timestamp = new DateTimeOffset(2018, 01, 01, 0, 10, 0, TimeSpan.Zero)
                };
                locationStore.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult(stored));
                var logger = Mock.Of<IDigitLogger>();
                var locationService = new LocationService(IntegratePushSyncService(new DateTimeOffset(2018, 01, 01, 0, 5, 0, TimeSpan.Zero)), locationStore.Object, logger);
                var res = await locationService.RequestLocationAsync(userId, new DateTimeOffset(2018, 01, 01, 1, 0, 0, TimeSpan.Zero), null);
                Assert.True(res.LocationRequestSent);
                Assert.Equal(new DateTimeOffset(2018, 01, 01, 1, 0, 0, TimeSpan.Zero), res.LocationRequestTime);
            }

            [Fact]
            public async void RequestLocation_StoredValid()
            {
                var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStore.Setup(v => v.GetNonExpiredGeofenceRequests(userId, It.IsAny<DateTimeOffset>())).Returns(Task.FromResult(new GeofenceRequest[0]));
                locationStore.Setup(v => v.SetGeofenceRequests(userId, It.IsAny<GeofenceRequest[]>())).Returns(Task.CompletedTask);
                var stored = new Location()
                {
                    Latitude = 40,
                    Longitude = 41,
                    Timestamp = new DateTimeOffset(2018, 01, 01, 0, 59, 0, TimeSpan.Zero)
                };
                locationStore.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult(stored));
                var pushSyncService = new Mock<IPushSyncService>(MockBehavior.Strict);
                var logger = Mock.Of<IDigitLogger>();
                var locationService = new LocationService(pushSyncService.Object, locationStore.Object, logger);
                var res = await locationService.RequestLocationAsync(userId, new DateTimeOffset(2018, 01, 01, 1, 0, 0, TimeSpan.Zero), null);
                Assert.False(res.LocationRequestSent);
                Assert.Null(res.LocationRequestTime);
            }

            [Fact]
            public async void RequestLocation_RequestPending()
            {
                var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStore.Setup(v => v.GetNonExpiredGeofenceRequests(userId, It.IsAny<DateTimeOffset>())).Returns(Task.FromResult(new GeofenceRequest[0]));
                locationStore.Setup(v => v.SetGeofenceRequests(userId, It.IsAny<GeofenceRequest[]>())).Returns(Task.CompletedTask);
                var stored = new Location()
                {
                    Latitude = 40,
                    Longitude = 41,
                    Timestamp = new DateTimeOffset(2018, 01, 01, 0, 0, 0, TimeSpan.Zero)
                };
                locationStore.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult(stored));
                var logger = Mock.Of<IDigitLogger>();
                var locationService = new LocationService(IntegratePushSyncService(new DateTimeOffset(2018, 01, 01, 0, 45, 0, TimeSpan.Zero)), locationStore.Object, logger);
                var res = await locationService.RequestLocationAsync(userId, new DateTimeOffset(2018, 01, 01, 0, 55, 0, TimeSpan.Zero), null);
                Assert.False(res.LocationRequestSent);
                Assert.Equal(new DateTimeOffset(2018, 01, 01, 0, 45, 0, TimeSpan.Zero), res.LocationRequestTime);
            }
        }

        public class LocationUpdateReceived
        {
            private readonly ILocationStore locationStore;

            private readonly DateTimeOffset now = new DateTimeOffset(2018, 1, 1, 10, 0, 0, TimeSpan.Zero);
            private readonly IDigitLogger logger;
            private readonly IPushSyncService pushSyncService;

            public LocationUpdateReceived()
            {
                var locationStoreMock = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStoreMock.Setup(v => v.GetNonExpiredGeofenceRequests(userId, It.IsAny<DateTimeOffset>())).Returns(Task.FromResult(new GeofenceRequest[0]));
                locationStoreMock.Setup(v => v.SetGeofenceRequests(userId, It.IsAny<GeofenceRequest[]>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.UpdateLocationAsync(userId, It.IsAny<Location>())).Returns(Task.CompletedTask);
                //locationStoreMock.Setup(v => v.SetGeofenceRequestedAsync(userId, It.IsAny<GeofenceRequest>())).Returns(Task.CompletedTask);
                //locationStoreMock.Setup(v => v.IsGeofenceActiveAsync(userId, It.IsAny<GeofenceRequest>())).Returns(Task.FromResult(false));
                //locationStoreMock.Setup(v => v.ClearGeofenceAsync(userId)).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult((Location)null));
                locationStore = locationStoreMock.Object;

                var pushSyncServiceMock = new Mock<IPushSyncService>(MockBehavior.Strict);
                pushSyncServiceMock.Setup(v => v.SetRequestedExternal(It.IsAny<string>(), It.IsAny<ISyncRequest>()))
                    .Returns(Task.CompletedTask);
                pushSyncServiceMock.Setup(v => v.SetDone(It.IsAny<string>(), It.IsAny<ISyncRequest>()))
                    .Returns(Task.CompletedTask);
                pushSyncServiceMock.Setup(v => v.SetDone(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(Task.CompletedTask);
                logger = Mock.Of<IDigitLogger>();
                pushSyncService = pushSyncServiceMock.Object;
            }

            private FocusItemWithExternalData GetDeparture(DateTimeOffset departureTime, DateTimeOffset firstStopTime, Event evt = null)
            {
                return new FocusItemWithExternalData()
                {
                    IndicateTime = departureTime,
                    DirectionsMetadata = new DirectionsMetadata()
                    {
                        PeferredRoute = 0
                    },
                    Directions = new TransitDirections()
                    {
                        Routes = new[] {
                            new Route()
                    {
                                StartLocation = new TravelService.Models.Coordinate(1,2),
                                EndLocation = new TravelService.Models.Coordinate(3,4),
                        DepatureTime = departureTime,
                        Steps = new[]{ new Step() {
                            DepartureTime = firstStopTime,
                            DepartureStop = new Stop()
                            {
                                Location = new TravelService.Models.Coordinate(5,6)
                            }
                        } }
                    }
                        }
                    },
                    CalendarEvent = evt
                };
            }

            [Fact]
            public async void LocationUpdateReceived_NoDepartures_NoUpdateRequired()
            {
                var locationService = new LocationService(pushSyncService, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {

                }, now, new FocusManageResult()
                {
                    ActiveItems = new List<FocusItemWithExternalData>()
                });
                Assert.Null(response.NextUpdateRequiredAt);
            }

            [Fact]
            public async void LocationUpdateReceived_UpcomingDeparture_LocationUpdateRequired()
            {
                var locationService = new LocationService(pushSyncService, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    ActiveItems = new List<FocusItemWithExternalData>()
                {
                    GetDeparture(now.AddMinutes(30), now.AddMinutes(45))
                }
                });
                Assert.Equal(now.AddMinutes(-2).AddMinutes(16), response.NextUpdateRequiredAt);
            }

            [Fact]
            public async void LocationUpdateReceived_PendingDeparture_UpdateForFirstStep()
            {
                var locationService = new LocationService(pushSyncService, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    ActiveItems = new List<FocusItemWithExternalData>()
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
                var locationService = new LocationService(pushSyncService, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    ActiveItems = new List<FocusItemWithExternalData>()
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
                var locationService = new LocationService(pushSyncService, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    ActiveItems = new List<FocusItemWithExternalData>()
                {
                       GetDeparture(now.AddMinutes(-2), now.AddMinutes(10)),
                }
                });
                Assert.Null(response.NextUpdateRequiredAt);
            }

            [Fact]
            public async void LocationUpdateReceived_UpcomingDeparture_NoGeofenceRequested()
            {
                var locationService = new LocationService(pushSyncService, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    ActiveItems = new List<FocusItemWithExternalData>()
                {
                    GetDeparture(now.AddMinutes(30), now.AddMinutes(45))
                }
                });
                Assert.Equal(0, response.Geofences.Length);
            }

            [Fact]
            public async void LocationUpdateReceived_PendingDeparture_GeofenceRequested()
            {
                var locationService = new LocationService(pushSyncService, locationStore, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    ActiveItems = new List<FocusItemWithExternalData>()
                {
                    GetDeparture(now.AddMinutes(9), now.AddMinutes(24), new Event() {
                        End = now.AddMinutes(60)
                    })
                }
                });
                Assert.NotEmpty(response.Geofences);
                Assert.Equal(now.AddMinutes(9).AddMinutes(-15), response.Geofences[0].Start);
            }

            [Fact]
            public async void LocationUpdateReceived_PendingDeparture_GeofenceActive()
            {
                var locationStoreMock = new Mock<ILocationStore>(MockBehavior.Strict);
                locationStoreMock.Setup(v => v.GetNonExpiredGeofenceRequests(userId, It.IsAny<DateTimeOffset>())).Returns(Task.FromResult(new GeofenceRequest[0]));
                locationStoreMock.Setup(v => v.SetGeofenceRequests(userId, It.IsAny<GeofenceRequest[]>())).Returns(Task.CompletedTask);
                locationStoreMock.Setup(v => v.UpdateLocationAsync(userId, It.IsAny<Location>())).Returns(Task.CompletedTask);
                //locationStoreMock.Setup(v => v.SetGeofenceRequestedAsync(userId, It.IsAny<GeofenceRequest>())).Returns(Task.CompletedTask);
                //locationStoreMock.Setup(v => v.IsGeofenceActiveAsync(userId, It.Is<GeofenceRequest>(d => d.Start == now && d.End == now.AddMinutes(60)))).Returns(Task.FromResult(true));
                //locationStoreMock.Setup(v => v.IsGeofenceActiveAsync(userId, It.Is<GeofenceRequest>(d => d.Start == now && d.End == now))).Returns(Task.FromResult(true));
                locationStoreMock.Setup(v => v.GetLastLocationAsync(userId)).Returns(Task.FromResult((Location)null));
                var locationService = new LocationService(pushSyncService, locationStoreMock.Object, logger);
                var response = await locationService.LocationUpdateReceivedAsync(userId, new Location()
                {
                    Timestamp = now.AddMinutes(-2)
                }, now, new FocusManageResult()
                {
                    ActiveItems = new List<FocusItemWithExternalData>()
                {
                    GetDeparture(now.AddMinutes(9), now.AddMinutes(24), new Event() {
                        End = now.AddMinutes(60)
                    })
                }
                });
                Assert.Equal(3, response.Geofences.Length);
            }
        }
    }
}
