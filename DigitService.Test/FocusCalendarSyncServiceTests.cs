using CalendarService.Client;
using CalendarService.Models;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitService.Controllers;
using DigitService.Models;
using DigitService.Service;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DigitService.Test
{
    public class FocusCalendarSyncServiceTests
    {
        const string userId = "12345";
        [Fact]
        public async void NewEvent_Added()
        {
            var focusStore = new Mock<IFocusStore>(MockBehavior.Strict);
            focusStore.Setup(v => v.GetCalendarItemsAsync(userId)).Returns(Task.FromResult(new FocusItem[0]));
            focusStore.Setup(v => v.StoreCalendarEventAsync(userId, It.IsAny<Event>())).Returns<string, Event>(async (v, e) => new FocusItem()
            {
                CalendarEventId = e.Id
            });
            var calendarService = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
            calendarService
                .Setup(v => v.Users[userId].Events.Get(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .Returns(Task.FromResult(new Event[] {
               new Event()
               {
                   Id = "Test1"
               }
            }));
            IFocusCalendarSyncService focusCalendarSyncService = new FocusCalendarSyncService(calendarService.Object, focusStore.Object);
            var res = await focusCalendarSyncService.SyncAsync(userId);
            Assert.Collection(res.AddedItems, v => Assert.Equal("Test1", v.CalendarEventId));
            Assert.Empty(res.ChangedItems);
            Assert.Empty(res.RemovedItems);
        }
        [Fact]
        public async void Events_Added_Removed()
        {
            var focusStore = new Mock<IFocusStore>(MockBehavior.Strict);
            focusStore.Setup(v => v.GetCalendarItemsAsync(userId)).Returns(Task.FromResult(new FocusItem[] {
                new FocusItem()
                {
                    CalendarEventId = "Test2"
                }
            }));
            focusStore.Setup(v => v.StoreCalendarEventAsync(userId, It.IsAny<Event>())).Returns<string, Event>(async (v, e) => new FocusItem()
            {
                CalendarEventId = e.Id
            });
            focusStore.Setup(v => v.RemoveAsync(It.IsAny<FocusItem>())).Returns(Task.CompletedTask);
            var calendarService = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
            calendarService
                .Setup(v => v.Users[userId].Events.Get(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .Returns(Task.FromResult(new Event[] {
               new Event()
               {
                   Id = "Test1"
               }
            }));
            IFocusCalendarSyncService focusCalendarSyncService = new FocusCalendarSyncService(calendarService.Object, focusStore.Object);
            var res = await focusCalendarSyncService.SyncAsync(userId);
            Assert.Collection(res.AddedItems, v => Assert.Equal("Test1", v.CalendarEventId));
            Assert.Collection(res.RemovedItems, v => Assert.Equal("Test2", v.CalendarEventId));
            Assert.Empty(res.ChangedItems);
        }

        [Fact]
        public async void Events_Unchanged()
        {
            var focusStore = new Mock<IFocusStore>(MockBehavior.Strict);
            var event1 = new Event()
            {
                Id = "Test1",
                Start = new DateTimeOffset(2018, 1, 1, 10, 0, 0, TimeSpan.Zero),
                Location = new LocationData()
                {
                    Text = "Location1"
                }
            };
            focusStore.Setup(v => v.GetCalendarItemsAsync(userId)).Returns(Task.FromResult(new FocusItem[] {
                new FocusItem()
                {
                    CalendarEventId = "Test1",
                    CalendarEventHash = event1.GenerateHash()
                }
            }));
            var calendarService = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
            calendarService
                .Setup(v => v.Users[userId].Events.Get(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .Returns(Task.FromResult(new Event[] {
                    event1
            }));
            IFocusCalendarSyncService focusCalendarSyncService = new FocusCalendarSyncService(calendarService.Object, focusStore.Object);
            var res = await focusCalendarSyncService.SyncAsync(userId);
            Assert.Empty(res.AddedItems);
            Assert.Empty(res.RemovedItems);
            Assert.Empty(res.ChangedItems);
        }

        [Fact]
        public async void Event_Changed()
        {
            var focusStore = new Mock<IFocusStore>(MockBehavior.Strict);
            var event1 = new Event()
            {
                Id = "Test1",
                Start = new DateTimeOffset(2018, 1, 1, 10, 0, 0, TimeSpan.Zero),
                Location = new LocationData()
                {
                    Text = "Location1"
                }
            };
            focusStore.Setup(v => v.GetCalendarItemsAsync(userId)).Returns(Task.FromResult(new FocusItem[] {
                new FocusItem()
                {
                    CalendarEventId = "Test1",
                    CalendarEventHash = event1.GenerateHash()
                }
            }));
            focusStore.Setup(v => v.UpdateCalendarEventAsync(userId, It.IsAny<Event>())).Returns<string, Event>(async (s, e) =>
                new FocusItem()
                {
                    Id = "Test1",
                    CalendarEventHash = e.GenerateHash()
                });
            var calendarService = new Mock<ICalendarServiceClient>(MockBehavior.Strict);
            calendarService
                .Setup(v => v.Users[userId].Events.Get(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .Returns(Task.FromResult(new Event[] {
                    new Event()
            {
                Id = "Test1",
                Start = new DateTimeOffset(2019, 1, 1, 10, 0, 0, TimeSpan.Zero),
                Location = new LocationData()
                {
                    Text = "Location2"
                }
            }
            }));
            IFocusCalendarSyncService focusCalendarSyncService = new FocusCalendarSyncService(calendarService.Object, focusStore.Object);
            var res = await focusCalendarSyncService.SyncAsync(userId);
            Assert.Empty(res.AddedItems);
            Assert.Empty(res.RemovedItems);
            Assert.Collection(res.ChangedItems, v => Assert.Equal("Test1", v.Id));
        }
    }
}
