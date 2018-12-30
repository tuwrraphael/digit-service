using CalendarService.Models;
using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IFocusStore
    {
        Task<FocusItem> StoreCalendarEventAsync(string userId, Event evt);
        Task<FocusItem> UpdateCalendarEventAsync(string userId, Event evt);
        Task<FocusItem> GetForCalendarEventAsync(string userId, Event evt);
        Task<bool> FocusItemNotifiedAsync(string itemId);
        Task SetFocusItemNotifiedAsync(string itemId);
    }
}
