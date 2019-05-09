using Digit.Focus.Model;
using DigitService.Models;
using System;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IFocusCalendarSyncService
    {
        Task<FocusItemSyncResult> SyncAsync(string userId, DateTimeOffset from, DateTimeOffset to);
    }
}
