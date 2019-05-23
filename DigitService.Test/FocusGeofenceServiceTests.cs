using CalendarService.Models;
using Digit.Abstractions.Service;
using Digit.Focus.Model;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitService.Impl;
using DigitService.Impl.EF;
using DigitService.Models;
using DigitService.Service;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var options = new DbContextOptionsBuilder<DigitServiceContext>()
               .UseInMemoryDatabase(databaseName: "GetGeofencesForActiveNavigations")
               .Options;

            // Run the test against one instance of the context
            using (var context = new DigitServiceContext(options))
            {
                context.FocusItems.Add(new StoredFocusItem()
                {
                    Id = "1",
                    UserId = userId,
                    Geofences = new List<StoredGeoFence> { new StoredGeoFence()
                    {
                        FocusItemId = "1",
                        Id = "start#1",
                        Start = new DateTimeOffset(2019,5,17,0,0,0, TimeSpan.Zero).AddMinutes(-15).UtcDateTime,
                        Lat = 3,
                        Lng = 4,
                        Radius = 50,
                        Exit = true
                    },
                    new StoredGeoFence()
                    {
                        FocusItemId = "1",
                        Id = "step1#1",
                        Start = new DateTimeOffset(2019,5,17,0,0,0, TimeSpan.Zero).AddMinutes(-15).UtcDateTime,
                        Lat = 3,
                        Lng = 4,
                        Radius = 150
                    } }
                });
                await context.SaveChangesAsync();
                var locationStore = new LocationStore(context, Mock.Of<IUserRepository>(), Mock.Of<IDigitLogger>());
                var svc = new FocusGeofenceService(Mock.Of<IDigitLogger>(), locationStore,
                    Mock.Of<IFocusStore>());
                var res = await svc.GetNewGeofencesForActiveNavigations(userId, new FocusManageResult()
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
                var stepFence = res.Where(v => v.Id == "step0#1").Single();
                Assert.Equal(7, stepFence.Lat);
                Assert.Equal(8, stepFence.Lng);
            }
        }
    }
}
