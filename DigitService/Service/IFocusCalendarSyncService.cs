using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IFocusCalendarSyncService
    {
        Task<FocusCalendarSyncResult> SyncAsync(string userId);
    }
}
