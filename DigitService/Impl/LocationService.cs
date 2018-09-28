using DigitPushService.Client;
using DigitService.Models;
using DigitService.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class LocationService : ILocationService
    {
        private static readonly ConcurrentDictionary<string, LocationUserService> locationUserServices = new ConcurrentDictionary<string, LocationUserService>();
        private readonly IPushService pushService;
        private readonly IDigitLogger logger;
        private readonly IUserService userService;
        private readonly IDigitPushServiceClient digitPushServiceClient;

        public LocationService(IPushService pushService, IDigitLogger logger, IUserService userService, IDigitPushServiceClient digitPushServiceClient)
        {
            this.pushService = pushService;
            this.logger = logger;
            this.userService = userService;
            this.digitPushServiceClient = digitPushServiceClient;
        }

        public async Task<Location> GetCurrentLocation(string userId)
        {
            var locationUserService = locationUserServices.GetOrAdd(userId, new LocationUserService(userId));
            return await locationUserService.GetCurrentLocation(SendPushNotification);
        }
        private async Task SendPushNotification(string userId)
        {
            var type = await pushService.GetPushRegistrationType(userId);
            if (PushRegistrationType.Legacy == type)
            {
                await pushService.Push(userId, new PushPayload() { Action = PushActions.SendLocation });
            }
            else if (PushRegistrationType.PushServer == type)
            {
                await digitPushServiceClient.Push[userId].Create(new DigitPushService.Models.PushRequest()
                {
                    ChannelOptions = new Dictionary<string, string>()
                    {
                        { "digitLocationRequest", null}
                    },
                    Payload = JsonConvert.SerializeObject(new PushPayload() { Action = PushActions.SendLocation })
                });
            }
            else
            {
                throw new UserConfigurationException("No push channel configured for user");
            }
        }

        public async Task LocationCallback(string userId, Location location)
        {
            var locationUserService = locationUserServices.GetOrAdd(userId, new LocationUserService(userId));
            await locationUserService.Set(location);
        }

        public async Task LocationConfigurationErrorCallback(string userId, LocationConfigurationError locationError)
        {
            var locationUserService = locationUserServices.GetOrAdd(userId, new LocationUserService(userId));
            await locationUserService.Set(null);
            await logger.Log(userId, "Location configuration error", 3);
        }

        private class LocationUserService
        {
            private static readonly Queue<TaskCompletionSource<Location>> taskCompletionSources = new Queue<TaskCompletionSource<Location>>();
            private static readonly SemaphoreSlim sem = new SemaphoreSlim(1);
            private readonly string userId;
            private Location resolvedLocation = null;
            private const int Expiration = 10 * 60;
            private const int TimeoutSeconds = 45;

            public LocationUserService(string userId)
            {
                this.userId = userId;
            }

            public async Task<Location> GetCurrentLocation(Func<string, Task> sendPushNotification)
            {
                Task<Location> result;
                await sem.WaitAsync();
                try
                {
                    if (null != resolvedLocation && (DateTime.Now - resolvedLocation.Timestamp).TotalSeconds < Expiration)
                    {
                        return new Location(resolvedLocation);
                    }
                    else
                    {
                        if (!taskCompletionSources.Any())
                        {
                            await sendPushNotification(userId);
                            Timer timer = new Timer(async state =>
                            {
                                await sem.WaitAsync();
                                try
                                {
                                    while (taskCompletionSources.TryDequeue(out TaskCompletionSource<Location> toRelease))
                                    {
                                        toRelease.TrySetException(new TimeoutException());
                                    }
                                }
                                finally
                                {
                                    sem.Release();
                                }
                            }, null, TimeoutSeconds * 1000, Timeout.Infinite);
                        }
                        var tcs = new TaskCompletionSource<Location>();
                        taskCompletionSources.Enqueue(tcs);
                        result = tcs.Task;
                    }
                }
                finally
                {
                    sem.Release();
                }
                return await result;
            }

            public async Task Set(Location location)
            {
                try
                {
                    await sem.WaitAsync();
                    resolvedLocation = location;
                    while (taskCompletionSources.TryDequeue(out TaskCompletionSource<Location> toRelease))
                    {
                        toRelease.SetResult(new Location(location));
                    }
                }
                finally
                {
                    sem.Release();
                }
            }
        }
    }
}
