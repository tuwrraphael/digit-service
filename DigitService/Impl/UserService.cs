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
        private readonly IDigitLogger digitLogger;
        private readonly DigitServiceOptions options;
        private const uint ReminderTime = 60;

        public UserService(IUserRepository userRepository,
            ICalendarService calendarService,
            IButler butler,
            IOptions<DigitServiceOptions> optionsAccessor,
            IDigitLogger digitLogger)
        {
            this.userRepository = userRepository;
            this.calendarService = calendarService;
            this.butler = butler;
            this.digitLogger = digitLogger;
            options = optionsAccessor.Value;
        }

        public async Task<UserInformation> CreateAsync(string userId)
        {
            if (null != await userRepository.GetAsync(userId))
            {
                throw new InvalidOperationException("User exists");
            }
            var user = await userRepository.CreateUser(userId);
            await digitLogger.Log(userId, "Created account", 1);
            return await MaintainAsync(userId);
        }

        private async Task InstallButlerForReminderRenewal(ReminderRegistration registration)
        {
            await butler.InstallAsync(new WebhookRequest()
            {
                When = registration.Expires.AddMinutes(-1),
                Data = new RenewReminderRequest()
                {
                    ReminderId = registration.Id
                },
                Url = options.ReminderMaintainanceCallbackUri
            });
        }

        public async Task<UserInformation> MaintainAsync(string userId)
        {
            var user = await userRepository.GetAsync(userId);
            if (null == user)
            {
                return null;
            }
            bool reminderAlive = false;
            if (null == user.ReminderId || !await calendarService.ReminderAliveAsync(userId, user.ReminderId))
            {
                ReminderRegistration registration = null;
                try
                {
                    registration = await calendarService.RegisterReminder(userId, ReminderTime);
                    await digitLogger.Log(userId, "Registered reminder", 1);
                }
                catch
                {
                    await digitLogger.Log(userId, "Could not register reminder", 3);
                }
                if (null != registration)
                {
                    user.ReminderId = registration.Id;
                    await userRepository.StoreReminderIdAsync(userId, user.ReminderId);
                    await InstallButlerForReminderRenewal(registration);
                }
            }
            {
                reminderAlive = true;
            }
            var userInformation = new UserInformation()
            {
                PushChannelRegistered = null != user.PushChannel,
                CalendarReminderActive = reminderAlive
            };
            return userInformation;
        }

        public async Task RenewReminder(string userId, RenewReminderRequest request)
        {
            var registration = await calendarService.RenewReminder(userId, request);
            await digitLogger.Log(userId, "Renewed reminder", 1);
            await InstallButlerForReminderRenewal(registration);
        }

        public async Task<UserInformation> GetInformationAsync(string userId)
        {
            var user = await userRepository.GetAsync(userId);
            if (null == user)
            {
                return null;
            }
            bool reminderAlive = false;
            if (null != user.ReminderId)
            {
                reminderAlive = await calendarService.ReminderAliveAsync(user.Id, user.ReminderId);
            }
            var userInformation = new UserInformation()
            {
                PushChannelRegistered = null != user.PushChannel,
                CalendarReminderActive = reminderAlive
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

        public async Task<string> GetUserIdForReminderAsync(string reminderId)
        {
            var user = await userRepository.GetByReminder(reminderId);
            if (null != user)
            {
                return user.Id;
            }
            return null;
        }
    }
}
