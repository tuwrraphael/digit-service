using CalendarService.Models;
using Digit.Focus.Models;

namespace Digit.Focus.Model
{
    public class FocusItemSyncResult
    {
        public FocusItem[] AddedItems { get; set; }
        public FocusItem[] RemovedItems { get; set; }
        public FocusItem[] ChangedItems { get; set; }
        public Event[] Events { get; set; }
    }

    public static class FocusItemSyncResultExtensions
    {
        public static bool ItemsUpdated(this FocusItemSyncResult s)
        {
            return s.AddedItems.Length > 0 || s.ChangedItems.Length > 0;
        }

        public static bool AnyChanges(this FocusItemSyncResult s)
        {
            return s.AddedItems.Length > 0 || s.ChangedItems.Length > 0
                || s.RemovedItems.Length > 0;
        }
    }
}
