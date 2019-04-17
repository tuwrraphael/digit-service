using Digit.Abstractions.Service;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using DigitPushService.Client;
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
        private readonly IDigitLogger logger;

        public DebouncedPushService(IDigitPushServiceClient digitPushServiceClient, IDigitLogger logger)

        {
            this.digitPushServiceClient = digitPushServiceClient;
            this.logger = logger;
        }

        public async Task PushDebounced(string userId, ISyncRequest syncRequest)
        {

            var locationUserService = locationUserServices.GetOrAdd(userId, new DebouncedPushUserService(userId, digitPushServiceClient, logger));
            await locationUserService.EnqueuePush(syncRequest);
        }

        private class DebouncedPushUserService
        {
            private static readonly Queue<ISyncRequest> syncRequests = new Queue<ISyncRequest>();
            private static readonly SemaphoreSlim sem = new SemaphoreSlim(1);

            private readonly string userId;
            private readonly IDigitPushServiceClient digitPushServiceClient;
            private readonly IDigitLogger logger;
            private Timer _timer;
            private static readonly TimeSpan DebounceTime = TimeSpan.FromSeconds(15);

            public DebouncedPushUserService(string userId,
                IDigitPushServiceClient digitPushServiceClient,
                IDigitLogger logger)
            {

                this.userId = userId;
                this.digitPushServiceClient = digitPushServiceClient;
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
                            if (syncRequests.Count > 0)
                            {
                                var channels = await digitPushServiceClient.PushChannels[userId].GetAllAsync();
                                var grouped = syncRequests.GroupBy(s =>
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
                                        await logger.Log(userId, $"Pushed {string.Join(", ", group.Select(d => d.Id))}", 1);
                                    }
                                    catch (PushChannelNotFoundException)
                                    {
                                        await logger.Log(userId, $"Could not find channel {group.Key}", 3);
                                    }
                                    catch (Exception e)
                                    {
                                        await logger.Log(userId, $"Could not push {string.Join(", ", group.Select(d => d.Id))}; Error: {e.Message}", 3);
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
                syncRequests.Enqueue(req);
                sem.Release();
            }
        }
    }
}
