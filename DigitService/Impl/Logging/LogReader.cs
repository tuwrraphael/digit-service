using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Digit.Abstractions.Models;
using DigitService.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DigitService.Impl.Logging
{

    public partial class LogReader : ILogReader, IRealtimeLogSubscriber
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ApplicationInsightsLogReader _applicationInsightsLogReader;
        private readonly ApplicationInsightsClientOptions _options;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _usersems = new ConcurrentDictionary<string, SemaphoreSlim>();
        private string UserLogKey(string userId) => $"log:{userId}";

        public LogReader(IMemoryCache memoryCache,
            IOptions<ApplicationInsightsClientOptions> optionsAccessor,
            ApplicationInsightsLogReader applicationInsightsLogReader)
        {
            _memoryCache = memoryCache;
            _applicationInsightsLogReader = applicationInsightsLogReader;
            _options = optionsAccessor.Value;
        }
        public async Task Add(LogEntry entry)
        {
            if (null == entry.UserId)
            {
                return;
            }
            var sem = _usersems.GetOrAdd(entry.UserId, new SemaphoreSlim(1));
            try
            {
                await sem.WaitAsync();
                _memoryCache.TryGetValue(UserLogKey(entry.UserId), out CachedLog cachedLog);
                cachedLog = cachedLog ?? new CachedLog();
                cachedLog.Log.Insert(0, entry);
                _memoryCache.Set(UserLogKey(entry.UserId), cachedLog);
            }
            finally
            {
                sem.Release();
            }
        }

        public Task<LogEntry[]> GetFocusItemLog(string userId, string focusItemId)
        {
            throw new NotImplementedException();
        }

        public async Task<LogEntry[]> GetUserLog(string userId)
        {
            var sem = _usersems.GetOrAdd(userId, new SemaphoreSlim(1));
            try
            {
                await sem.WaitAsync();
                var hasCachedLog = _memoryCache.TryGetValue(UserLogKey(userId), out CachedLog cachedLog);
                cachedLog = cachedLog ?? new CachedLog();
                var since = DateTimeOffset.Now - TimeSpan.FromDays(1);
                if (!hasCachedLog || !cachedLog.AILogSince.HasValue || cachedLog.AILogSince >= since)
                {
                    var userAiLog = await _applicationInsightsLogReader.GetUserAILog(_options.ApplicationID, userId);
                    cachedLog.Log = userAiLog.Union(cachedLog.Log.Where(v => !userAiLog.Any(aiEntry => aiEntry.Id == v.Id)))
                        .OrderByDescending(v => v.Timestamp).ToList();
                    cachedLog.AILogSince = since;
                }
                _memoryCache.Set(UserLogKey(userId), cachedLog);
                return cachedLog.Log.ToArray();
            }
            finally
            {
                sem.Release();
            }
        }

        private class CachedLog
        {
            public DateTimeOffset? AILogSince { get; set; }
            public List<LogEntry> Log { get; set; }
            public CachedLog()
            {
                Log = new List<LogEntry>();
            }
        }
    }
}