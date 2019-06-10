using Digit.Abstractions.Models;
using System;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface ILogReader
    {
        Task<LogEntry[]> GetUserLog(string userId);
        Task<LogEntry[]> GetFocusItemLog(string userId, string focusItemId);
    }
}
