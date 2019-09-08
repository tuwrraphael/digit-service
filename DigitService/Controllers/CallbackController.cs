using CalendarService.Client;
using CalendarService.Models;
using Digit.Abstractions.Service;
using Digit.Focus.Model;
using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TravelService.Models;
using TravelService.Models.Directions;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private readonly IUserService userService;
        private readonly IDigitLogger digitLogger;
        private readonly IFocusService focusService;

        public CallbackController(IUserService userService,
            IDigitLogger digitLogger,
            IFocusService focusService)
        {
            this.userService = userService;
            this.digitLogger = digitLogger;
            this.focusService = focusService;
        }

        [HttpPost("reminder")]
        [AllowAnonymous]
        public async Task<IActionResult> ReminderCallback([FromBody]ReminderDelivery reminderDelivery)
        {
            var userId = reminderDelivery.ClientState;
            await focusService.ReminderDeliveryAsync(userId, reminderDelivery);
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
                await digitLogger.LogErrorForUser(userId, $"Could not renew reminder.");
            }
            return Ok();
        }

        [HttpPost("notify-user")]
        [AllowAnonymous]
        public async Task<IActionResult> NotifyUserCallback([FromBody]NotifyUserRequest notifyUserRequest)
        {
            await focusService.NotifyCallbackAsync(notifyUserRequest);
            return Ok();
        }

        [HttpPost("directions")]
        [AllowAnonymous]
        public async Task<IActionResult> DirectionsCallback([FromBody]DirectionsUpdate update)
        {
            await focusService.DirectionsCallbackAsync(update);
            return Ok();
        }
    }
}
