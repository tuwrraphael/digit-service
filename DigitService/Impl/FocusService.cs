using CalendarService.Models;
using Digit.Abstractions.Service;
using Digit.Focus;
using Digit.Focus.Model;
using Digit.Focus.Service;
using DigitService.Models;
using DigitService.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class FocusService : IFocusService
    {
        private readonly IDigitLogger logger;
        private readonly IFocusCalendarSyncService focusCalendarSyncService;
        private readonly ILocationService locationService;
        private readonly IFocusUpdateService focusUpdateService;
        private readonly IFocusNotificationService _focusNotificationService;

        public FocusService(IDigitLogger logger,
            IFocusCalendarSyncService focusCalendarSyncService,
            ILocationService locationService,
            IFocusUpdateService focusUpdateService,
            IFocusNotificationService focusNotificationService)
        {
            this.logger = logger;
            this.focusCalendarSyncService = focusCalendarSyncService;
            this.locationService = locationService;
            this.focusUpdateService = focusUpdateService;
            _focusNotificationService = focusNotificationService;
        }


        public async Task NotifyCallbackAsync(NotifyUserRequest request)
        {
            await _focusNotificationService.Notify(request);
        }

        public async Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location)
        {
            await logger.LogForUser(userId, $"Received Location");
            var manageResult = await focusUpdateService.Update(userId, new FocusUpdateRequest()
            {
                ItemSyncResult = null,
                Location = location
            });
            return await locationService.LocationUpdateReceivedAsync(userId, location, DateTimeOffset.Now, manageResult);
        }

        public async Task ReminderDeliveryAsync(string userId, ReminderDelivery reminderDelivery)
        {
            if (!reminderDelivery.Removed)
            {
                await logger.LogForUser(userId, $"Received reminder for {reminderDelivery.Event.Subject}");
            }
            else
            {
                await logger.LogForUser(userId, $"Received reminder removed");
            }
            var syncResult = await focusCalendarSyncService.SyncAsync(userId, DateTimeOffset.Now,
                DateTimeOffset.Now + FocusConstants.FocusScanTime + FocusConstants.CalendarServiceInacurracy);
            var lastLocation = await locationService.GetLastLocationAsync(userId);
            var res = await focusUpdateService.Update(userId, new FocusUpdateRequest()
            {
                ItemSyncResult = syncResult,
                Location = lastLocation
            });
            // for now request anyway, there item might be unchanged due to planner
            await locationService.RequestLocationAsync(userId, DateTimeOffset.Now, res);
        }

        public async Task PatchAsync(string userId)
        {
            await focusUpdateService.Update(userId, new FocusUpdateRequest()
            {
                ItemSyncResult = null,
                Location = await locationService.GetLastLocationAsync(userId)
            });
        }
    }
}
