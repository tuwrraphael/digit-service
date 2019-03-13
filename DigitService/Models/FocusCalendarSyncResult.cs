using Digit.Focus.Models;

namespace DigitService.Models
{
    public class FocusCalendarSyncResult
    {
        public FocusItem[] AddedItems { get; set; }
        public FocusItem[] RemovedItems { get; set; }
        public FocusItem[] ChangedItems { get; set; }
    }
}
