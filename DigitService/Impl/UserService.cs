﻿using ButlerClient;
using CalendarService.Client;
using CalendarService.Models;
using DigitService.Models;
using DigitService.Service;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class UserService : IUserService
    {
        private readonly IUserRepository userRepository;
        private readonly ICalendarServiceClient calendarService;
        private readonly IButler butler;
        private readonly IDigitLogger digitLogger;
        private readonly IPushService pushService;
        private readonly DigitServiceOptions options;
        private const uint ReminderTime = 120;
        private static ConcurrentDictionary<string, SemaphoreSlim> maintainanceSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        public UserService(IUserRepository userRepository,
            ICalendarServiceClient calendarService,
            IButler butler,
            IOptions<DigitServiceOptions> optionsAccessor,
            IDigitLogger digitLogger,
            IPushService pushService)
        {
            this.userRepository = userRepository;
            this.calendarService = calendarService;
            this.butler = butler;
            this.digitLogger = digitLogger;
            this.pushService = pushService;
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
                When = registration.Expires.AddMinutes(-4),
                Data = new RenewReminderRequest()
                {
                    ReminderId = registration.Id
                },
                Url = options.ReminderMaintainanceCallbackUri
            });
        }

        public async Task<UserInformation> MaintainAsync(string userId)
        {
            var sm = maintainanceSemaphores.GetOrAdd(userId, (key) => new SemaphoreSlim(1));
            try
            {
                await sm.WaitAsync();
                var user = await userRepository.GetAsync(userId);
                if (null == user)
                {
                    return null;
                }
                bool reminderInactive = null == user.ReminderId || !await calendarService.ReminderAliveAsync(userId, user.ReminderId);
                if (reminderInactive)
                {
                    ReminderRegistration registration = null;
                    try
                    {
                        registration = await calendarService.RegisterReminderAsync(userId, new ReminderRequest() {
                            Minutes = ReminderTime,
                            ClientState = userId,
                            NotificationUri = options.ReminderCallbackUri
                        });
                        await digitLogger.Log(userId, "Registered reminder", 1);
                    }
                    catch
                    {
                        await digitLogger.Log(userId, "Could not register reminder", 3);
                    }
                    if (null != registration)
                    {
                        reminderInactive = false;
                        user.ReminderId = registration.Id;
                        await userRepository.StoreReminderIdAsync(userId, user.ReminderId);
                        await InstallButlerForReminderRenewal(registration);
                    }
                }
                var userInformation = new UserInformation()
                {
                    PushChannelRegistered = await PushChannelRegistered(userId),
                    CalendarReminderActive = !reminderInactive
                };

                return userInformation;
            }
            finally
            {
                sm.Release();
            }
        }

        public async Task RenewReminder(string userId, RenewReminderRequest request)
        {
            var registration = await calendarService.RenewReminderAsync(userId, request.ReminderId);
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
                PushChannelRegistered = await PushChannelRegistered(userId),
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

        private async Task<bool> PushChannelRegistered(string userId)
        {
            return await pushService.GetPushRegistrationType(userId) != PushRegistrationType.None;
        }
    }
}
