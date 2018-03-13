using DigitService.Models;
using DigitService.Service;
using Microsoft.EntityFrameworkCore;
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

        public async Task<User> CreateUser(string userId)
        {
            var u = new User()
            {
                Id = userId
            };
            context.Users.Add(u);
            await context.SaveChangesAsync();
            return u;
        }

        public async Task<User> GetAsync(string userId)
        {
            return await context.Users.SingleOrDefaultAsync(v => v.Id == userId);
        }

        public async Task StorePushChannelAsync(string userId, string channelId)
        {
            var user = await context.Users.SingleAsync(v => v.Id == userId);
            user.PushChannel = channelId;
            await context.SaveChangesAsync();
        }

        public async Task StoreReminderIdAsync(string userId, string reminderId)
        {
            var user = await context.Users.SingleAsync(v => v.Id == userId);
            user.ReminderId = reminderId;
            await context.SaveChangesAsync();
        }
    }
}
