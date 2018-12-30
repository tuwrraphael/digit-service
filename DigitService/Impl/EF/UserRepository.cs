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

        public async Task<User> GetByReminder(string reminderId)
        {
            return await context.Users.SingleOrDefaultAsync(v => v.ReminderId == reminderId);
        }

        public async Task<User> GetOrCreateAsync(string userId)
        {
            var user = await GetAsync(userId);
            if (null == user)
            {
                user = await CreateUser(userId);
            }
            return user;
        }

        public async Task StoreReminderIdAsync(string userId, string reminderId)
        {
            var user = await context.Users.SingleAsync(v => v.Id == userId);
            user.ReminderId = reminderId;
            await context.SaveChangesAsync();
        }
    }
}
