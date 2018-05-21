using CalendarService.Client;
using DigitService.Impl;
using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using TravelService.Client;
using TravelService.Models.Directions;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private readonly IUserService userService;
        private readonly IDigitLogger digitLogger;
        private readonly ICalendarServiceClient calendarServiceClient;
        private readonly ITravelServiceClient travelServiceClient;

        public CallbackController(IUserService userService,
            IDigitLogger digitLogger,
            ICalendarServiceClient calendarServiceClient,
            ITravelServiceClient travelServiceClient)
        {
            this.userService = userService;
            this.digitLogger = digitLogger;
            this.calendarServiceClient = calendarServiceClient;
            this.travelServiceClient = travelServiceClient;
        }

        [HttpPost("reminder")]
        [AllowAnonymous]
        public async Task<IActionResult> ReminderCallback([FromBody]ReminderDelivery reminderDelivery)
        {
            var userId = reminderDelivery.ClientState;
            var timeToStart = reminderDelivery.Event.Start - DateTime.Now;
            TransitDirections directions = null;
            if (!string.IsNullOrEmpty(reminderDelivery.Event.Location))
            {
                try
                {
                    directions = await travelServiceClient.Directions.Transit.Get(reminderDelivery.Event.Location, reminderDelivery.Event.Start - new TimeSpan(0, 2, 0), userId);
                }
                catch (TravelServiceException ex)
                {
                    await digitLogger.Log(userId, $"Error while retrieving directions: {ex.Message}");
                }
            }
            if (null != directions && directions.Routes.Where(v => v.DepatureTime.HasValue).Any())
            {
                var route = directions.Routes.Where(v => v.DepatureTime.HasValue).First();
                var leaveTime = route.DepatureTime.Value - DateTime.Now;
                var dirText = string.Join(' ', route.Steps.Select(v => $"Take {v.Line.ShortName} at {v.DepartureStop.Name} heading {v.Headsign} at {new DateTimeOffset(v.DepartureTime).ToOffset(new TimeSpan(2, 0, 0)).ToString("HH:mm")}.").ToArray());
                await digitLogger.Log(userId, $"Leave for {reminderDelivery.Event.Subject} in {Math.Round(leaveTime.TotalMinutes)} minutes. {dirText}");
            }
            else
            {
                await digitLogger.Log(userId, $"Event {reminderDelivery.Event.Subject} starts in {Math.Round(timeToStart.TotalMinutes)} minutes.");
            }
            return Ok();
        }

        [HttpPost("reminder-maintainance")]
        [AllowAnonymous]
        public async Task<IActionResult> ReminderMaintainanceCallback([FromBody]RenewReminderRequest renewReminderRequest)
        {
            var userId = await userService.GetUserIdForReminderAsync(renewReminderRequest.ReminderId);
            if (null == userId)
            {
                return NotFound();
            }
            try
            {
                await userService.RenewReminder(userId, renewReminderRequest);
            }
            catch (CalendarServiceException)
            {
                await digitLogger.Log(userId, $"Could not renew reminder.", 3);
            }
            return Ok();
        }
    }
}
