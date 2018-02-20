using DigitService.Models;
using System.Threading.Tasks;

namespace DigitService.Service
{
    public interface IDeviceRepository
    {
        Task<LogEntry> LogAsync(string deviceId, LogEntry entry);
        Task<LogEntry[]> GetLogAsync(string deviceId, int history = 15);
    }
}