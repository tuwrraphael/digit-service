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
                Id = storedFocusItem.Id
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
            return item.UserNotified;
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
                UserNotified = false
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
            await digitServiceContext.SaveChangesAsync();
            return item.MapToFocusItem();
        }
    }
}
