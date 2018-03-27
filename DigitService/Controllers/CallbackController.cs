using DigitService.Impl;
using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private readonly IUserService userService;
        private readonly IDigitLogger digitLogger;
        private readonly ICalendarService calendarService;

        public CallbackController(IUserService userService,
            IDigitLogger digitLogger,
            ICalendarService calendarService)
        {
            this.userService = userService;
            this.digitLogger = digitLogger;
            this.calendarService = calendarService;
        }

        [HttpPost("reminder")]
        [AllowAnonymous]
        public async Task<IActionResult> ReminderCallback([FromBody]ReminderDelivery reminderDelivery)
        {
            var userId = reminderDelivery.ClientState;
            var timeToStart = reminderDelivery.Event.Start - DateTime.Now;
            await digitLogger.Log(userId, $"Event {reminderDelivery.Event.Subject} starts in {Math.Round(timeToStart.TotalMinutes)} minutes.");
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
            catch (ReminderException)
            {
                await digitLogger.Log(userId, $"Could not renew reminder.", 3);
            }
            return Ok();
        }
    }
}
