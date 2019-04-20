using CalendarService.Models;
using Digit.Focus;
using Digit.Focus.Models;
using Digit.Focus.Service;
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
                CalendarEventFeedId = storedFocusItem.CalendarEventFeedId,
                CalendarEventHash = storedFocusItem.CalendarEvent.CalendarEventHash,
                Start = storedFocusItem.ActiveStart,
                End = storedFocusItem.ActiveEnd
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
            return (await digitServiceContext.FocusItems
                .Include(v => v.CalendarEvent)
                .Where(v => v.UserId == userId &&
                 DateTimeOffset.Now.AddHours(-2).UtcDateTime <= v.ActiveStart && DateTimeOffset.Now.UtcDateTime <= v.ActiveEnd)
                .ToArrayAsync())
                .Select(v => v.MapToFocusItem()).ToArray();
        }

        public async Task<FocusItem[]> GetCalendarItemsAsync(string userId)
        {
            return (await digitServiceContext.FocusItems
                .Include(v => v.CalendarEvent)
                .Where(v => v.UserId == userId && null != v.CalendarEventFeedId && null != v.CalendarEventId)
            .ToArrayAsync())
            .Select(v => v.MapToFocusItem()).ToArray();
        }
        public async Task RemoveAsync(FocusItem evt)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == evt.Id).SingleOrDefaultAsync();
            digitServiceContext.FocusItems.Remove(item);
            await digitServiceContext.SaveChangesAsync();
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
                    Id = evt.Id,
                    CalendarEventHash = evt.GenerateHash()
                },
                User = user,
                UserNotified = false,
                ActiveEnd = evt.End.UtcDateTime,
                ActiveStart = (evt.Start - FocusConstants.CalendarServiceInacurracy).UtcDateTime,
                IndicateAt = evt.Start.UtcDateTime,
                DirectionsKey = null
            };
            await digitServiceContext.FocusItems.AddAsync(focusItem);
            await digitServiceContext.SaveChangesAsync();
            return focusItem.MapToFocusItem();
        }

        public async Task<bool> UpdateActiveItem(string userId, string itemId)
        {
            var user = await digitServiceContext.Users
                .Where(v => v.Id == userId).SingleOrDefaultAsync();
            bool matches = false;
            if (null == user)
            {
                return false;
            }
            else
            {
                matches = itemId == user.ActiveFocusItem;
                user.ActiveFocusItem = itemId;
            }
            await digitServiceContext.SaveChangesAsync();
            return !matches;
        }

        public async Task<FocusItem> UpdateCalendarEventAsync(string userId, Event evt)
        {
            var item = await digitServiceContext.FocusItems
                .Include(v => v.CalendarEvent)
                .Where(v => v.UserId == userId
                        && v.CalendarEventFeedId == evt.FeedId
                        && v.CalendarEventId == evt.Id)
                        .SingleOrDefaultAsync();
            item.UserNotified = false;
            item.ActiveEnd = evt.End.UtcDateTime;
            item.ActiveStart = (evt.Start - FocusConstants.CalendarServiceInacurracy).UtcDateTime;
            item.CalendarEvent.CalendarEventHash = evt.GenerateHash();
            await digitServiceContext.SaveChangesAsync();
            return item.MapToFocusItem();
        }

        public async Task UpdateDirections(string itemId, string directionsKey)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == itemId).SingleOrDefaultAsync();
            item.DirectionsKey = directionsKey;
            await digitServiceContext.SaveChangesAsync();
        }

        public async Task UpdateIndicateTime(string itemId, DateTimeOffset indicateTime)
        {
            var item = await digitServiceContext.FocusItems.Where(v => v.Id == itemId).SingleOrDefaultAsync();
            item.IndicateAt = indicateTime.UtcDateTime;
            await digitServiceContext.SaveChangesAsync();
        }
    }
}
