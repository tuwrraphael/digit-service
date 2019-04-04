using CalendarService.Models;
using Digit.Abstractions.Service;
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

        public FocusService(IDigitLogger logger,
            IFocusCalendarSyncService focusCalendarSyncService,
            ILocationService locationService,
            IFocusUpdateService focusUpdateService)
        {
            this.logger = logger;
            this.focusCalendarSyncService = focusCalendarSyncService;
            this.locationService = locationService;
            this.focusUpdateService = focusUpdateService;
        }


        public async Task NotifyCallbackAsync(NotifyUserRequest request)
        {
            await focusUpdateService.Update(request.UserId, new FocusUpdateRequest()
            {
                ItemSyncResult = null,
                Location = await locationService.GetLastLocationAsync(request.UserId)
            });
        }

        public async Task<LocationResponse> LocationUpdateReceivedAsync(string userId, Location location)
        {
            await logger.Log(userId, $"Received Location");
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
                await logger.Log(userId, $"Received reminder for {reminderDelivery.Event.Subject}");
            }
            else
            {
                await logger.Log(userId, $"Received reminder removed");
            }
            var syncResult = await focusCalendarSyncService.SyncAsync(userId);
            var lastLocation = await locationService.GetLastLocationAsync(userId);
            var res = await focusUpdateService.Update(userId, new FocusUpdateRequest()
            {
                ItemSyncResult = syncResult,
                Location = lastLocation
            });
            if (syncResult.AddedItems.Any() || syncResult.ChangedItems.Any())
            {
                await locationService.RequestLocationAsync(userId, DateTimeOffset.Now, res);
            }
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
