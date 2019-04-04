using System.Linq;
using System.Threading.Tasks;
using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Microsoft.EntityFrameworkCore;

namespace Digit.DeviceSynchronization.Impl
{
    public class DeviceSyncStore : IDeviceSyncStore
    {
        private readonly DeviceSynchronizationContext _deviceSynchronizationContext;

        public DeviceSyncStore(DeviceSynchronizationContext deviceSynchronizationContext)
        {
            _deviceSynchronizationContext = deviceSynchronizationContext;
        }

        public async Task CreateAsync(string userId, string deviceId)
        {
            await _deviceSynchronizationContext.Devices.AddAsync(new StoredDevice()
            {
                Id = deviceId,
                OwnerId = userId
            });
            await _deviceSynchronizationContext.SaveChangesAsync();
        }

        public async Task<string> DeviceClaimedByAsync(string deviceId)
        {
            return await _deviceSynchronizationContext.Devices.Where(d => d.Id == deviceId)
                .Select(v => v.OwnerId).SingleOrDefaultAsync();
        }

        public async Task<Device[]> GetForUserAsync(string userId)
        {
            return await _deviceSynchronizationContext.Devices.Where(d => d.OwnerId == userId)
                .Select(v => new Device()
                {
                    Id = v.Id,
                    OwnerId = v.OwnerId
                }).ToArrayAsync();
        }
    }
}
