using ButlerClient;
using CalendarService.Client;
using DigitPushService.Client;
using DigitPushService.Models;
using DigitService.Impl;
using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private readonly IDigitPushServiceClient digitPushServiceClient;
        private readonly IButler butler;
        private readonly DigitServiceOptions options;

        public CallbackController(IUserService userService,
            IDigitLogger digitLogger,
            ICalendarServiceClient calendarServiceClient,
            ITravelServiceClient travelServiceClient,
            IDigitPushServiceClient digitPushServiceClient,
            IButler butler,
            IOptions<DigitServiceOptions> optionsAccessor)
        {
            this.userService = userService;
            this.digitLogger = digitLogger;
            this.calendarServiceClient = calendarServiceClient;
            this.travelServiceClient = travelServiceClient;
            this.digitPushServiceClient = digitPushServiceClient;
            this.butler = butler;
            options = optionsAccessor.Value;
        }

        [HttpPost("reminder")]
        [AllowAnonymous]
        public async Task<IActionResult> ReminderCallback([FromBody]ReminderDelivery reminderDelivery)
        {
            var userId = reminderDelivery.ClientState;
            var timeToStart = reminderDelivery.Event.Start - DateTime.Now;
            TransitDirections directions = null;
            if (null != reminderDelivery.Event.Location)
            {
                try
                {
                    var address = reminderDelivery.Event.Location.Address != null ?
                        $"{reminderDelivery.Event.Location.Address.Street}, {reminderDelivery.Event.Location.Address.PostalCode} {reminderDelivery.Event.Location.Address.City} {reminderDelivery.Event.Location.Address.CountryOrRegion}" : reminderDelivery.Event.Location.Text;
                    directions = await travelServiceClient.Directions.Transit.Get(address, reminderDelivery.Event.Start - new TimeSpan(0, 2, 0), userId);
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
                await butler.InstallAsync(new WebhookRequest()
                {
                    When = route.DepatureTime.Value,
                    Data = new NotifyUserRequest()
                    {
                        UserId = userId,
                        Message = JsonConvert.SerializeObject(new { notification = new {
                            title = $"Losgehen zu {reminderDelivery.Event.Subject}",
                            body = $"Mach dich auf den Weg. {reminderDelivery.Event.Subject} beginnt in {(reminderDelivery.Event.Start - route.DepatureTime.Value).TotalMinutes} Minuten." } })
                    },
                    Url = options.NotifyUserCallbackUri
                });
            }
            else
            {
                await digitLogger.Log(userId, $"Event {reminderDelivery.Event.Subject} starts in {Math.Round(timeToStart.TotalMinutes)} minutes.");
                await butler.InstallAsync(new WebhookRequest()
                {
                    When = reminderDelivery.Event.Start - new TimeSpan(0,45,0),
                    Data = new NotifyUserRequest()
                    {
                        UserId = userId,
                        Message = JsonConvert.SerializeObject(new
                        {
                            notification = new
                            {
                                title = $"Losgehen zu {reminderDelivery.Event.Subject}",
                                body = $"Mach dich auf den Weg. {reminderDelivery.Event.Subject} beginnt in 45 Minuten."
                            }
                        })
                    },
                    Url = options.NotifyUserCallbackUri
                });
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

        [HttpPost("notify-user")]
        [AllowAnonymous]
        public async Task<IActionResult> NotifyUserCallback([FromBody]NotifyUserRequest notifyUserRequest)
        {
            try
            {
                await digitPushServiceClient.Push[notifyUserRequest.UserId].Create(new PushRequest()
                {
                    ChannelOptions = new Dictionary<string, string>() { { "digit.notify", null } },
                    Payload = notifyUserRequest.Message
                });
            }
            catch (Exception e)
            {
                await digitLogger.Log(notifyUserRequest.UserId, $"Could notify user ({e.Message}).", 3);
            }
            return Ok();
        }
    }
}
