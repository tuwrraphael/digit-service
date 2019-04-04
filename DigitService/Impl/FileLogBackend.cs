using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Digit.Abstractions.Models;
using DigitService.Hubs;
using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;

namespace DigitService.Impl
{
    public class FileLogBackend : ILogBackend
    {
        private readonly IFileProvider provider;
        private readonly IHubContext<LogHub> context;
        private const string LogCollection = "Logs";
        private const string DeviceConfigCollection = "Device";

        private static ConcurrentDictionary<string, DeviceSynchronization> deviceSynchronizations = new ConcurrentDictionary<string, DeviceSynchronization>();

        public FileLogBackend(IFileProvider provider, IHubContext<LogHub> context)
        {
            this.provider = provider;
            this.context = context;
        }

        public async Task<LogEntry[]> GetLogAsync(string deviceId, int history = 15)
        {
            var logList = new Queue<string>(history);
            var synchro = deviceSynchronizations.GetOrAdd(deviceId, new DeviceSynchronization());
            try
            {
                await synchro.SemaphoreSlim.WaitAsync();
                var fileInfo = provider.GetFileInfo($"{deviceId}-{"Log"}.json");
                if (!fileInfo.Exists)
                {
                    return null;
                }
                else
                {
                    using (var stream = fileInfo.CreateReadStream())
                    using (var reader = new StreamReader(stream))
                    {
                        string line = null;
                        while (null != (line = await reader.ReadLineAsync()))
                        {
                            if (logList.Count >= history)
                            {
                                logList.Dequeue();
                            }
                            logList.Enqueue(line);
                        }
                    }
                }
            }
            finally
            {
                synchro.SemaphoreSlim.Release();
            }
            var regex = new Regex("(^(\\s*)\\,(\\s*))|(^(\\s*)\\[(\\s*))|((\\s*),(\\s*)$)|((\\s*)](\\s*)$)");
            return logList.Select(v => regex.Replace(v, String.Empty)).Select(JsonConvert.DeserializeObject<LogEntry>).ToArray();
        }

        public async Task<LogEntry> LogAsync(string deviceId, LogEntry entry)
        {
            entry.LogTime = DateTime.Now;
            entry.Id = Guid.NewGuid().ToString();
            var synchro = deviceSynchronizations.GetOrAdd(deviceId, new DeviceSynchronization());
            var json = JsonConvert.SerializeObject(entry);
            try
            {
                await synchro.SemaphoreSlim.WaitAsync();
                var fileInfo = provider.GetFileInfo($"{deviceId}-{"Log"}.json");
                using (var stream = new FileStream(fileInfo.PhysicalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        if (stream.Length != 0)
                        {
                            stream.Seek(-1, SeekOrigin.End);
                            await writer.WriteAsync($",{Environment.NewLine}{json}]");
                        }
                        else
                        {
                            await writer.WriteAsync($"[{json}]");
                        }

                    }
                }
                await context.Clients.All.SendAsync("log", entry);
            }
            finally
            {
                synchro.SemaphoreSlim.Release();
            }
            synchro.AutoResetEvent.Set();
            return entry;
        }
    }
}