using ButlerClient;
using DigitService.Models;
using DigitService.Service;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly ICalendarService calendarService;
        private readonly IButler butler;
        private readonly DigitServiceOptions options;
        private const uint ReminderTime = 60;

        public UserService(IUserRepository userRepository,
            ICalendarService calendarService,
            IButler butler,
            IOptions<DigitServiceOptions> optionsAccessor)
        {
            this.userRepository = userRepository;
            this.calendarService = calendarService;
            this.butler = butler;
            options = optionsAccessor.Value;
        }

        public async Task<UserInformation> CreateAsync(string userId)
        {
            if (null == await userRepository.GetAsync(userId))
            {
                throw new InvalidOperationException("User exists");
            }
            var user = await userRepository.CreateUser(userId);
            return await MaintainAsync(userId);
        }

        public async Task<UserInformation> MaintainAsync(string userId)
        {
            var user = await userRepository.GetAsync(userId);
            if (null == user)
            {
                return null;
            }
            if (null == user.ReminderId || !await calendarService.ReminderAliveAsync(userId, user.ReminderId))
            {
                ReminderRegistration registration = null;
                try
                {
                    registration = await calendarService.RegisterReminder(userId, ReminderTime);
                }
                catch
                {
                }
                if (null != registration)
                {
                    user.ReminderId = registration.Id;
                    await userRepository.StoreReminderIdAsync(userId, user.ReminderId);
                    await butler.InstallAsync(new WebhookRequest()
                    {
                        When = registration.Expires.AddMinutes(-5),
                        Data = new RenewReminderRequest()
                        {
                            ReminderId = user.ReminderId
                        },
                        Url = options.ReminderMaintainanceCallbackUri
                    });
                }
            }
            var userInformation = new UserInformation()
            {
                PushChannelRegistered = null != user.PushChannel,
                CalendarReminderActive = null != user.ReminderId
            };
            return userInformation;
        }

        public async Task<UserInformation> GetInformationAsync(string userId)
        {
            var user = await userRepository.GetAsync(userId);
            var userInformation = new UserInformation()
            {
                PushChannelRegistered = null != user.PushChannel,
                CalendarReminderActive = null != user.ReminderId
            };
            return userInformation;
        }

        public async Task RegisterPushChannel(string userId, string channelId)
        {
            if (null == await userRepository.GetAsync(userId))
            {
                await CreateAsync(userId);
            }
            await userRepository.StorePushChannelAsync(userId, channelId);
        }
    }
}
