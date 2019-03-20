using Digit.Abstractions.Models;
using DigitService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface ILogBackend
    {
        Task<LogEntry> LogAsync(string deviceId, LogEntry entry);
        Task<LogEntry[]> GetLogAsync(string deviceId, int history = 15);
    }
}
