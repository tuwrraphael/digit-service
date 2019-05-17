using CalendarService.Client;
using Digit.Focus.Models;
using Digit.Focus.Service;
using System.Threading.Tasks;
using TravelService.Client;

namespace DigitService.Impl
{
    public class FocusExternalDataService : IFocusExternalDataService
    {
        private readonly ICalendarServiceClient _calendarServiceClient;
        private readonly ITravelServiceClient _travelServiceClient;
        private readonly IFocusStore _focusStore;

        public FocusExternalDataService(ICalendarServiceClient calendarServiceClient,
            ITravelServiceClient travelServiceClient, IFocusStore focusStore)
        {
            _calendarServiceClient = calendarServiceClient;
            _travelServiceClient = travelServiceClient;
            _focusStore = focusStore;
        }

        public async Task<FocusItemWithExternalData> Get(string userId, string id)
        {
            var item = await _focusStore.Get(userId, id);
            if (null == item)
            {
                return null;
            }
            return new FocusItemWithExternalData()
            {
                CalendarEvent = null == item.CalendarEventId && null == item.CalendarEventFeedId ?
                    null : await _calendarServiceClient.Users[userId].Feeds[item.CalendarEventFeedId].Events.Get(item.CalendarEventId),
                Directions = null == item.DirectionsMetadata ? null :
                    (await _travelServiceClient.Directions[item.DirectionsMetadata.Key].GetAsync()).TransitDirections,
                DirectionsMetadata = item.DirectionsMetadata,
                End = item.End,
                Id = item.Id,
                IndicateTime = item.IndicateTime,
                Start = item.Start
            };
        }
    }
}
