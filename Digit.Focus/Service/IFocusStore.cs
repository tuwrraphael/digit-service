using CalendarService.Models;
using Digit.Focus.Models;
using System;
using System.Threading.Tasks;

namespace Digit.Focus.Service
{
    public interface IFocusStore
    {
        Task<FocusItem> StoreCalendarEventAsync(string userId, Event evt);
        Task<FocusItem> UpdateCalendarEventAsync(string userId, Event evt);
        Task<bool> FocusItemNotifiedAsync(string itemId);
        Task SetFocusItemNotifiedAsync(string itemId);
        Task UpdateWithDirections(string itemId, DateTimeOffset indicateTime, string directionsKey);
        Task<FocusItem[]> GetActiveAsync(string userId);
        Task<FocusItem[]> GetCalendarItemsAsync(string userId);
        Task RemoveAsync(FocusItem evt);
    }
}
