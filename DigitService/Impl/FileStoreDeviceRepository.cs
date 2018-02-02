using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FileStore;
using Microsoft.Extensions.FileProviders;
using Models;
using Newtonsoft.Json;
using Service;

namespace Impl
{
    public class FileDeviceRepository : IDeviceRepository
    {
        private readonly IFileStore store;
        private readonly IFileProvider provider;
        private const string LogCollection = "Logs";
        private const string DeviceConfigCollection = "Device";

        private ConcurrentDictionary<string, DeviceSynchronization> deviceSynchronizations;

        public FileDeviceRepository(IFileProvider provider)
        {

            deviceSynchronizations = new ConcurrentDictionary<string, DeviceSynchronization>();
            this.provider = provider;
        }

        public async Task<LogEntry[]> GetLogAsync(string deviceId, int history = 15)
        {
            var logEntryList = new Queue<LogEntry>(history);
            var synchro = deviceSynchronizations.GetOrAdd(deviceId, new DeviceSynchronization());
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
                        var cleared = new Regex("(^(\\s*)\\,(\\s*))|(^(\\s*)\\[(\\s*))|((\\s*),(\\s*)$)|((\\s*)](\\s*)$)").Replace(line, String.Empty);
                        if (logEntryList.Count >= history)
                        {
                            logEntryList.Dequeue();
                        }
                        logEntryList.Enqueue(JsonConvert.DeserializeObject<LogEntry>(cleared));
                    }
                }
            }
            synchro.SemaphoreSlim.Release();
            return logEntryList.ToArray();
        }

        public async Task LogAsync(string deviceId, LogEntry entry)
        {
            entry.LogTime = DateTime.Now;
            entry.Id = Guid.NewGuid().ToString();
            var synchro = deviceSynchronizations.GetOrAdd(deviceId, new DeviceSynchronization());
            var fileInfo = provider.GetFileInfo($"{deviceId}-{"Log"}.json");
            var json = JsonConvert.SerializeObject(entry);
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
            await synchro.SemaphoreSlim.WaitAsync();
            synchro.SemaphoreSlim.Release();
            synchro.AutoResetEvent.Set();
        }

        public async Task ChangeDeviceConfigAsync(string deviceId, Func<DeviceConfig, DeviceConfig> configureAction)
        {
            //var config = await store.GetAsync<DeviceConfig>(deviceId, DeviceConfigCollection) ?? new DeviceConfig();
            //var updated = configureAction(config);
            //await store.SaveAsync(deviceId, DeviceConfigCollection, updated);
        }
    }
}