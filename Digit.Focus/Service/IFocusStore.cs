using CalendarService.Models;
using Digit.Focus.Models;
using System;
using System.Threading.Tasks;
using TravelService.Models.Directions;

namespace Digit.Focus.Service
{
    public interface IFocusStore
    {
        Task<FocusItem> StoreCalendarEventAsync(string userId, Event evt);
        Task<FocusItem> Get(string userId, string id);
        Task<FocusItem> UpdateCalendarEventAsync(string userId, Event evt);
        Task<bool> FocusItemNotifiedAsync(string itemId);
        Task SetFocusItemNotifiedAsync(string itemId);
        Task UpdateIndicateTime(string itemId, DateTimeOffset indicateTime);
        Task UpdateDirections(string itemId, DirectionsResult directionsResult, int? preferredRoute);
        Task<bool> UpdateActiveItem(string userId, string itemId);
        Task<FocusItem[]> GetActiveAsync(string userId);
        Task<FocusItem[]> GetTimeRangeAsync(string userId, DateTimeOffset from, DateTimeOffset to);
        Task<FocusItem[]> GetCalendarItemsAsync(string userId);
        Task RemoveAsync(FocusItem evt);
        Task SetPlaceForItem(string userId, string itemId, Place place);
        Task SetTravelStatus(string userId, string itemId, TravelStatus travelStatus);
    }
}
