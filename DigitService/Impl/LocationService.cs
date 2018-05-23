using DigitService.Extensions;
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

        public LocationService(IPushService pushService, IDigitLogger logger)
        {
            this.pushService = pushService;
            this.logger = logger;
        }

        public async Task<Location> GetCurrentLocation(string userId)
        {
            var locationUserService = locationUserServices.GetOrAdd(userId, new LocationUserService(userId));
            return await locationUserService.GetCurrentLocation(SendPushNotification);
        }
        private async Task SendPushNotification(string userId)
        {
            await pushService.Push(userId, new Models.PushPayload() { Action = "SendLocation" });
        }

        public async Task LocationCallback(string userId, Location location)
        {
            var locationUserService = locationUserServices.GetOrAdd(userId, new LocationUserService(userId));
            await locationUserService.Set(location);
        }

        private class LocationUserService
        {
            private static readonly Queue<TaskCompletionSource<Location>> taskCompletionSources = new Queue<TaskCompletionSource<Location>>();
            private static readonly SemaphoreSlim sem = new SemaphoreSlim(1);
            private readonly string userId;
            private Location resolvedLocation = null;
            private const int Expiration = 10;
            private const int Timeout = 30;

            public LocationUserService(string userId)
            {
                this.userId = userId;
            }

            public async Task<Location> GetCurrentLocation(Func<string, Task> sendPushNotification)
            {
                Task<Location> result;
                try
                {
                    await sem.WaitAsync();
                    if (null != resolvedLocation && (DateTime.Now - resolvedLocation.Timestamp).TotalSeconds < Expiration)
                    {
                        return new Location(resolvedLocation);
                    }
                    else
                    {
                        if (!taskCompletionSources.Any())
                        {
                            await sendPushNotification(userId);
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
                return await result.TimeoutAfter(Timeout * 1000);
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
