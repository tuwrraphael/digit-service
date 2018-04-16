using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class DeviceController : Controller
    {
        private readonly IDigitLogger digitLogger;
        private readonly IDeviceService deviceService;
        private readonly ILogBackend logBackend;

        public DeviceController(IDigitLogger digitLogger,
            IDeviceService deviceService, ILogBackend logBackend)
        {
            this.digitLogger = digitLogger;
            this.deviceService = deviceService;
            this.logBackend = logBackend;
        }


        [HttpPost("{id}/log")]
        public async void PostLog(string id, [FromBody]LogEntry entry)
        {
            await digitLogger.Log(id, entry);
        }

        [HttpGet("{id}/log")]
        public async Task<LogEntry[]> GetLog(string id, int history = 20)
        {
            return await logBackend.GetLogAsync(id, history);
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
