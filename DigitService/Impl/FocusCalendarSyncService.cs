using CalendarService.Client;
using CalendarService.Models;
using DigitService.Models;
using DigitService.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    public class FocusCalendarSyncService : IFocusCalendarSyncService
    {
        private readonly ICalendarServiceClient calendarServiceClient;
        private readonly IFocusStore focusStore;


        public FocusCalendarSyncService(ICalendarServiceClient calendarServiceClient,
            IFocusStore focusStore)
        {
            this.calendarServiceClient = calendarServiceClient;
            this.focusStore = focusStore;
        }

        public async Task<FocusCalendarSyncResult> SyncAsync(string userId)
        {
            var events = await calendarServiceClient.Users[userId].Events.Get(DateTimeOffset.Now, DateTimeOffset.Now.Add(FocusConstants.FocusScanTime + FocusConstants.CalendarServiceInacurracy)) ?? new CalendarService.Models.Event[0];
            var notAllDay = events.Where(v => !v.IsAllDay).ToArray();
            var focusItems = new List<FocusItem>(await focusStore.GetCalendarItemsAsync(userId));
            var newItems = new List<FocusItem>();
            var changedItems = new List<FocusItem>();
            foreach (var evt in notAllDay)
            {
                var focusItem = focusItems.FirstOrDefault(v => v.CalendarEventId == evt.Id && v.CalendarEventFeedId == evt.FeedId);
                if (null != focusItem)
                {
                    focusItems.Remove(focusItem);
                    if (focusItem.CalendarEventHash != evt.GenerateHash())
                    {
                        changedItems.Add(await focusStore.UpdateCalendarEventAsync(userId, evt));
                    }
                }
                else
                {
                    focusItem = await focusStore.StoreCalendarEventAsync(userId, evt);
                    newItems.Add(focusItem);
                }
            }
            foreach (var evt in focusItems)
            {
                await focusStore.RemoveAsync(evt);
            }
            return new FocusCalendarSyncResult()
            {
                AddedItems = newItems.ToArray(),
                ChangedItems = changedItems.ToArray(),
                RemovedItems = focusItems.ToArray()
            };
        }
    }
}
