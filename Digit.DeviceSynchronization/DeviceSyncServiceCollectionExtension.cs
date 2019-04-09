using Digit.DeviceSynchronization.Impl;
using Digit.DeviceSynchronization.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Digit.DeviceSynchronization
{
    public static class DeviceSyncServiceCollectionExtension
    {
        public static IServiceCollection AddDeviceSynchronization(this IServiceCollection services,
             Action<DbContextOptionsBuilder> ConfigureDbContext)
        {
            services.AddTransient<IPushSyncService, PushSyncService>();
            services.AddTransient<IPushSyncStore, PushSyncStore>();
            services.AddTransient<IDebouncedPushService, DebouncedPushService>();
            services.AddTransient<IDeviceDataService, DeviceDataService>();
            services.AddTransient<IDeviceSyncService, DeviceSyncService>();
            services.AddTransient<IDeviceSyncStore, DeviceSyncStore>();
            services.AddDbContext<DeviceSynchronizationContext>(ConfigureDbContext);
            return services;
        }
    }
}
