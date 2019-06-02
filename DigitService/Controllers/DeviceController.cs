using Digit.Abstractions.Models;
using Digit.Abstractions.Service;
using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class DeviceController : Controller
    {
        private readonly IDigitLogger digitLogger;
        private readonly IDeviceService deviceService;
        private readonly ILogReader _logReader;

        public DeviceController(IDigitLogger digitLogger,
            IDeviceService deviceService, ILogReader logReader)
        {
            this.digitLogger = digitLogger;
            this.deviceService = deviceService;
            _logReader = logReader;
        }


        [Obsolete]
        [Authorize("User")]
        [HttpPost("{id}/log")]
        public async void PostLog(string id, [FromBody]LegacyLogRequest entry)
        {
            await digitLogger.Log(User.GetId(), entry);
        }

        [Obsolete]
        [Authorize("User")]
        [HttpGet("{id}/log")]
        public async Task<LogEntry[]> GetLog(string id, [FromUri]TimeSpan? timespan)
        {
            return (await _logReader.GetUserLog(User.GetId(), timespan.GetValueOrDefault(TimeSpan.FromDays(1)))).Reverse().ToArray();
        }

        [HttpPost("{id}/claim")]
        [Authorize("User")]
        public async Task<IActionResult> Claim(string id)
        {
            var success = await deviceService.ClaimAsync(User.GetId(), id);
            if (success)
            {
                return Ok();
            }
            else
            {
                return BadRequest("Device already claimed");
            }
        }

        [HttpPost("{id}/battery")]
        [Authorize("User")]
        public async Task<IActionResult> AddBatteryMeasurement(string id, [FromBody] BatteryMeasurement batteryMeasurement)
        {
            if (!await deviceService.HasAccessAsync(id, User.GetId()))
            {
                return Unauthorized();
            }

            await deviceService.AddBatteryMeasurementAsync(id, batteryMeasurement);
            return Ok();
        }

        [HttpPost("{id}/battery/measure")]
        [Authorize("User")]
        public async Task<IActionResult> TriggerBatteryMeasurement(string id)
        {
            if (!await deviceService.HasAccessAsync(id, User.GetId()))
            {
                return Unauthorized();
            }
            throw new NotImplementedException();
        }

        [HttpGet("{id}")]
        [Authorize("User")]
        public async Task<IActionResult> GetStatus(string id)
        {
            if (!await deviceService.HasAccessAsync(id, User.GetId()))
            {
                return Unauthorized();
            }
            return Ok(await deviceService.GetDeviceStatusAsync(id));
        }

        [HttpGet()]
        [Authorize("User")]
        public async Task<IActionResult> GetDevices()
        {
            return Ok(await deviceService.GetDevices(User.GetId()));
        }
    }
}
