using CalendarService.Models;
using DigitService.Models;
using DigitService.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl.EF
{
    public static class StoredFocusItemExtension
    {
        public static void MapFromFocusItem(this StoredFocusItem storedFocusItem, FocusItem focusItem)
        {

        }

        public static FocusItem MapToFocusItem(this StoredFocusItem storedFocusItem)
        {
            return new FocusItem()
            {
                Id = storedFocusItem.Id,
                IndicateTime = new DateTimeOffset(storedFocusItem.IndicateAt, TimeSpan.Zero),
                DirectionsKey = storedFocusItem.DirectionsKey,
                CalendarEventId = storedFocusItem.CalendarEventId,
                CalendarEventFeedId = storedFocusItem.CalendarEventFeedId
            };
        }
    }


    public class FocusStore : IFocusStore
    {
        private readonly DigitServiceContext digitServiceContext;
        private readonly IUserRepository userRepository;

        public FocusStore(DigitServiceContext digitServiceContext, IUserRepository userRepository)
        {
            this.digitServiceContext = digitServiceContext;
            this.userRepository = userRepository;
        }


        public async Task<bool> FocusItemNotifiedAsync(string itemId)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == itemId).SingleOrDefaultAsync();
            await digitServiceContext.Entry(item).ReloadAsync();
            return item.UserNotified;
        }

        public async Task<FocusItem[]> GetActiveAsync(string userId)
        {
            return (await digitServiceContext.FocusItems.Where(v => v.UserId == userId && DateTime.UtcNow <= v.ActiveEnd)
                .ToArrayAsync())
                .Select(v => v.MapToFocusItem()).ToArray();
        }

        public async Task<FocusItem> GetForCalendarEventAsync(string userId, Event evt)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.UserId == userId
            && v.CalendarEventFeedId == evt.FeedId && v.CalendarEventId == evt.Id).SingleOrDefaultAsync();
            return item?.MapToFocusItem();
        }

        public async Task SetFocusItemNotifiedAsync(string itemId)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == itemId).SingleOrDefaultAsync();
            item.UserNotified = true;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task<FocusItem> StoreCalendarEventAsync(string userId, Event evt)
        {
            var user = await userRepository.GetOrCreateAsync(userId);
            var focusItem = new StoredFocusItem()
            {
                Id = Guid.NewGuid().ToString(),
                CalendarEvent = new StoredCalendarEvent()
                {
                    FeedId = evt.FeedId,
                    Id = evt.Id
                },
                User = user,
                UserNotified = false,
                ActiveEnd = evt.End.UtcDateTime,
                IndicateAt = evt.Start.UtcDateTime,
                DirectionsKey = null
            };
            await digitServiceContext.FocusItems.AddAsync(focusItem);
            await digitServiceContext.SaveChangesAsync();
            return focusItem.MapToFocusItem();
        }

        public async Task<FocusItem> UpdateCalendarEventAsync(string userId, Event evt)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.UserId == userId
                        && v.CalendarEventFeedId == evt.FeedId && v.CalendarEventId == evt.Id).SingleOrDefaultAsync();
            item.UserNotified = false;
            item.ActiveEnd = evt.End.UtcDateTime;
            await digitServiceContext.SaveChangesAsync();
            return item.MapToFocusItem();
        }

        public async Task UpdateWithDirections(string itemId, DateTimeOffset indicateTime, string directionsKey)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == itemId).SingleOrDefaultAsync();
            item.IndicateAt = indicateTime.UtcDateTime;
            item.DirectionsKey = directionsKey;
            await digitServiceContext.SaveChangesAsync();
        }
    }
}
