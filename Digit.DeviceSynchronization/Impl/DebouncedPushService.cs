using Digit.Abstractions.Models;
using Digit.Abstractions.Service;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using DigitPushService.Client;
using Microsoft.Extensions.Logging;
using PushServer.PushConfiguration.Abstractions.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Digit.DeviceSynchronization.Impl
{
    public class DebouncedPushService : IDebouncedPushService
    {
        private static readonly ConcurrentDictionary<string, DebouncedPushUserService> locationUserServices = new ConcurrentDictionary<string, DebouncedPushUserService>();
        private readonly IDigitPushServiceClient digitPushServiceClient;
        private readonly ILogger<DebouncedPushService> logger;
        private readonly IDigitLogger digitLogger;

        public DebouncedPushService(IDigitPushServiceClient digitPushServiceClient,
            IDigitLogger digitLogger,
            ILogger<DebouncedPushService> logger)

        {
            this.digitPushServiceClient = digitPushServiceClient;
            this.digitLogger = digitLogger;
            this.logger = logger;
        }

        public async Task PushDebounced(string userId, ISyncRequest syncRequest)
        {

            var locationUserService = locationUserServices.GetOrAdd(userId, 
                new DebouncedPushUserService(userId, digitPushServiceClient, digitLogger, logger));
            await locationUserService.EnqueuePush(syncRequest);
        }

        private class DebouncedPushUserService
        {
            private static readonly List<ISyncRequest> syncRequests = new List<ISyncRequest>();
            private static readonly SemaphoreSlim sem = new SemaphoreSlim(1);

            private readonly string userId;
            private readonly IDigitPushServiceClient digitPushServiceClient;
            private readonly IDigitLogger digitLogger;
            private readonly ILogger<DebouncedPushService> logger;
            private Timer _timer;
            private static readonly TimeSpan DebounceTime = TimeSpan.FromSeconds(15);

            public DebouncedPushUserService(string userId,
                IDigitPushServiceClient digitPushServiceClient,
                IDigitLogger digitLogger,
                ILogger<DebouncedPushService> logger)
            {

                this.userId = userId;
                this.digitPushServiceClient = digitPushServiceClient;
                this.digitLogger = digitLogger;
                this.logger = logger;
            }

            public async Task EnqueuePush(ISyncRequest req)
            {
                await sem.WaitAsync();
                if (null == _timer)
                {
                    _timer = new Timer(async state =>
                    {
                        await sem.WaitAsync();
                        try
                        {
                            var reqs = syncRequests.ToArray();
                            await digitLogger.LogForUser(userId, $"Enqueued {reqs.Length}");
                            if (reqs.Length > 0)
                            {
                                var channels = await digitPushServiceClient.PushChannels[userId].GetAllAsync();
                                var grouped = reqs.GroupBy(s =>
                                {
                                    var channelOptions = s.GetChannelOptions();
                                    IQueryable<PushChannelConfiguration> query = channels.AsQueryable();
                                    foreach (var option in channelOptions)
                                    {
                                        if (option.Value == null)
                                        {
                                            query = query.Where(v => v.Options.ContainsKey(option.Key));
                                        }
                                        else
                                        {
                                            query = query.Where(v => v.Options.ContainsKey(option.Key) && v.Options[option.Key] == option.Value);
                                        }
                                    }
                                    var channel = query.FirstOrDefault();
                                    return channel?.Id;
                                });
                                foreach (var group in grouped)
                                {
                                    try
                                    {
                                        await digitPushServiceClient.Push[userId].Create(new DigitPushService.Models.PushRequest()
                                        {
                                            Options = new PushServer.Models.PushOptions()
                                            {
                                                Urgency = PushServer.Models.PushUrgency.High
                                            },
                                            ChannelId = group.Key,
                                            Payload = "{\"action\" : \"digit.sync\"}"
                                        });
                                        await digitLogger.LogForUser(userId, $"Pushed {string.Join(", ", group.Select(d => d.Id))}", DigitTraceAction.RequestPush);
                                    }
                                    catch (PushChannelNotFoundException)
                                    {
                                        await digitLogger.LogForUser(userId, $"Could not find channel {group.Key}", logLevel: LogLevel.Error);
                                    }
                                    catch (Exception e)
                                    {
                                        logger.LogError(e, "push exception");
                                        await digitLogger.LogErrorForUser(userId, $"Could not push {string.Join(", ", group.Select(d => d.Id))}; Error: {e.Message}");
                                    }
                                }
                                syncRequests.Clear();
                            }
                        }
                        finally
                        {
                            _timer = null;
                            sem.Release();
                        }
                    }, null, (int)DebounceTime.TotalMilliseconds, Timeout.Infinite);
                }
                syncRequests.Add(req);
                await digitLogger.LogForUser(userId, $"Enqueued {req}");
                sem.Release();
            }
        }
    }
}
