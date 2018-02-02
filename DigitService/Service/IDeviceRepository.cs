using Models;
using System;
using System.Threading.Tasks;

namespace Service
{
    public interface IDeviceRepository
    {
        Task<LogEntry> LogAsync(string deviceId, LogEntry entry);
        Task<LogEntry[]> GetLogAsync(string deviceId, int history = 15);

        Task ChangeDeviceConfigAsync(string deviceId, Func<DeviceConfig, DeviceConfig> configureAction);
    }
}