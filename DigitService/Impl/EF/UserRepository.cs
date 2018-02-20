using DigitService.Models;
using DigitService.Service;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl.EF
{
    public class UserRepository : IUserRepository
    {
        private readonly DigitServiceContext context;

        public UserRepository(DigitServiceContext Context)
        {
            context = Context;
        }

        public async Task<DeviceClaimResult> ClaimDevice(string userId, string deviceId)
        {
            if (await context.Devices.Where(v => v.Id == deviceId && v.UserId != null).AnyAsync())
            {
                return DeviceClaimResult.DeviceAlreadyClaimed;
            }
            var dev = await context.Devices.Where(p => p.Id == deviceId).SingleOrDefaultAsync();
            if (null != dev)
            {
                dev.UserId = userId;
            }
            else
            {
                context.Devices.Add(new Device()
                {
                    Id = deviceId,
                    UserId = userId
                });
            }
            await context.SaveChangesAsync();
            return DeviceClaimResult.Success;
        }

        public async Task CreateUser(NewUser user)
        {
            var u = new User()
            {
                Id = user.Id
            };
            context.Users.Add(u);
            await context.SaveChangesAsync();
        }

        public async Task<bool> Exists(string userId)
        {
            return await context.Users.Where(p => p.Id == userId).AnyAsync();
        }

        public async Task<string> GetPushChannel(string userId)
        {
            return await context.Users.Where(p => p.Id == userId).Select(v => v.PushChannel).SingleOrDefaultAsync();
        }

        public async Task RegisterPushChannel(string userId, string registrationId)
        {
            var user = await context.Users.Where(p => p.Id == userId).SingleOrDefaultAsync();
            if (null == user)
            {
                await CreateUser(new NewUser() { Id = userId });
            }
            user = await context.Users.Where(p => p.Id == userId).SingleAsync();
            user.PushChannel = registrationId;
            await context.SaveChangesAsync();
        }
    }
}
