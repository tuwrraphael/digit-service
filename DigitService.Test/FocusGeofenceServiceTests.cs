using CalendarService.Models;
using Digit.Abstractions.Service;
using Digit.Focus.Model;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitService.Impl;
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
    public class FocusGeofenceServiceTests
    {
        string userId = "11234";

        [Fact]
        public async Task GetGeofencesForActiveNavigations()
        {

            var locationStore = new Mock<ILocationStore>(MockBehavior.Strict);
            locationStore.Setup(v => v.GetActiveGeofenceRequests(userId, It.IsAny<DateTimeOffset>()))
                .Returns(Task.FromResult(new[] {
                    new GeofenceRequest()
                    {
                        FocusItemId = "1",
                        Id = "start#1",
                        Start = new DateTimeOffset(2019,5,17,0,0,0, TimeSpan.Zero).AddMinutes(-15),
                        Lat = 3,
                        Lng = 4,
                        Radius = 50,
                        Exit = true
                    },
                    new GeofenceRequest()
                    {
                        FocusItemId = "1",
                        Id = "step1#1",
                        Start = new DateTimeOffset(2019,5,17,0,0,0, TimeSpan.Zero).AddMinutes(-15),
                        Lat = 3,
                        Lng = 4,
                        Radius = 150
                    }
                }));
            var svc = new FocusGeofenceService(Mock.Of<IDigitLogger>(), locationStore.Object,
                Mock.Of<IFocusStore>());
            var res = await svc.GetGeofencesForActiveNavigations(userId, new FocusManageResult()
            {
                ActiveItems = new List<FocusItemWithExternalData>()
                {
                    new FocusItemWithExternalData()
                    {
                        Id = "1",
                        DirectionsMetadata = new DirectionsMetadata()
                        {
                            PeferredRoute = 0
                        },
                        Directions = new TransitDirections()
                        {
                            Routes = new []{ new Route()
                            {
                                DepatureTime = new DateTimeOffset(2019,5,17,0,0,0, TimeSpan.Zero),
                                StartLocation = new TravelService.Models.Coordinate(3,4),
                                EndLocation = new TravelService.Models.Coordinate(5,6),
                                Steps = new []{
                                    new Step()
                                    {
                                        DepartureTime = new DateTimeOffset(2019,5,17,0,0,0, TimeSpan.Zero).AddMinutes(10),
                                        DepartureStop = new Stop()
                                        {
                                            Location = new TravelService.Models.Coordinate(7,8)
                                        }
                                    }
                                }
                            }
                        }
                        },
                        CalendarEvent = new Event()
                        {
                            End = new DateTimeOffset(2019,5,17,0,0,0, TimeSpan.Zero).AddMinutes(50)
                        }
                    },
                }
            }, new DateTimeOffset(2019, 5, 17, 0, 0, 0, TimeSpan.Zero));
            Assert.Collection(res, item => Assert.Equal("step0#1", item.Id),
                item => Assert.Equal("end#1", item.Id));
        }
    }
}
