using DigitService.Models;
using DigitService.Service;
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

        public LocationService(IPushService pushService, IDigitLogger logger, IUserService userService)
        {
            this.pushService = pushService;
            this.logger = logger;
            this.userService = userService;
        }

        public async Task<Location> GetCurrentLocation(string userId)
        {
            var locationUserService = locationUserServices.GetOrAdd(userId, new LocationUserService(userId));
            return await locationUserService.GetCurrentLocation(SendPushNotification);
        }
        private async Task SendPushNotification(string userId)
        {
            if (!await userService.PushChannelRegistered(userId))
            {
                throw new UserConfigurationException("No push channel configured for user");
            }
            await pushService.Push(userId, new PushPayload() { Action = PushActions.SendLocation });
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
