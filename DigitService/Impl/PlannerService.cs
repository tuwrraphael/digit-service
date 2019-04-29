using CalendarService.Models;
using Digit.Focus;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitService.Models;
using DigitService.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TravelService.Client;
using TravelService.Models.Directions;

namespace DigitService.Impl
{
    public class PlannerService : IPlannerService
    {
        private readonly IFocusCalendarSyncService _focusCalendarSyncService;
        private readonly ITravelServiceClient _travelServiceClient;
        private readonly IFocusStore _focusStore;

        public PlannerService(IFocusCalendarSyncService focusCalendarSyncService, ITravelServiceClient travelServiceClient,
            IFocusStore focusStore)
        {
            _focusCalendarSyncService = focusCalendarSyncService;
            _travelServiceClient = travelServiceClient;
            _focusStore = focusStore;
        }

        public async Task<Plan> GetPlan(string userId, DateTimeOffset from, DateTimeOffset to)
        {

            string start = "#home";

            var syncResult = await _focusCalendarSyncService.SyncAsync(userId, from, to);
            var items = (await _focusStore.GetTimeRangeAsync(userId, from, to)).OrderBy(d => d.Start);
            var directionsList = new List<DirectionsResult>();
            var focusItemsList = new List<FocusItem>();
            DateTimeOffset? getup = null;
            bool first = true;
            foreach (var e in items)
            {
                var calendarItem = syncResult.Events.Where(c => c.Id == e.CalendarEventId && c.FeedId == e.CalendarEventFeedId).SingleOrDefault();
                if (null != calendarItem)
                {
                    if (first)
                    {

                    }
                    focusItemsList.Add(e);
                    var address = calendarItem.GetFormattedAddress();
                    var directions = await _travelServiceClient.Users[userId]
                        .Directions.Transit.Get(start, address, calendarItem.Start);
                    var route = DirectionUtils.SelectRoute(directions);
                    if (null != route)
                    {
                        if (first)
                        {
                            getup = route.DepatureTime - TimeSpan.FromHours(1);
                        }
                        directionsList.Add(directions);
                        await _focusStore.UpdateDirections(e.Id, directions.CacheKey);
                        await _focusStore.UpdateIndicateTime(e.Id, route.DepatureTime);
                        e.IndicateTime = route.DepatureTime;
                        e.DirectionsKey = directions.CacheKey;
                    }
                    start = address;
                    first = false;
                }
            }
            var plan = new Plan()
            {
                Events = syncResult.Events,
                Directions = directionsList.ToArray(),
                FocusItems = items.ToArray(),
                GetUp = getup
            };
            return plan;
        }
    }
}
