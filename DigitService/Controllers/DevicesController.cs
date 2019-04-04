using Digit.DeviceSynchronization.Models;
using Digit.DeviceSynchronization.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api")]
    public class DevicesController : Controller
    {
        private readonly IDeviceSyncService _deviceSyncService;
        private readonly IDeviceDataService _deviceDataService;
        private readonly IPushSyncService _pushSyncService;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(IDeviceSyncService deviceSyncService,
            IDeviceDataService deviceDataService,
            IPushSyncService pushSyncService,
            ILogger<DevicesController> logger)
        {
            _deviceSyncService = deviceSyncService;
            _deviceDataService = deviceDataService;
            _pushSyncService = pushSyncService;
            _logger = logger;
        }

        [HttpPut("me/devices/{id}")]
        [Authorize("UserDevice")]
        public async Task<IActionResult> RequestSync(string id, [FromBody]DeviceSyncRequest deviceSyncRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            try
            {
                await _deviceSyncService.RequestSynchronizationAsync(User.GetId(), id, deviceSyncRequest);
                return Ok();
            }
            catch (DeviceClaimedException ex)
            {
                return Forbid();
            }
        }

        [HttpGet("devices/{id}/sync")]
        [Authorize("UserDevice")]
        public async Task<IActionResult> GetSyncStatus(string id)
        {
            try
            {
                var status = await _deviceDataService.GetDeviceSyncStatus(User.GetId(), id);
                if (null == status)
                {
                    return NotFound();
                }
                return Ok(status);
            }
            catch (DeviceAccessException e)
            {
                _logger.LogError("Invalid device status access", e);
                return Forbid();
            }

        }

        [HttpGet("devices/{id}/data")]
        [Authorize("UserDevice")]
        public async Task<IActionResult> GetData(string id)
        {
            try
            {
                var data = await _deviceDataService.GetDeviceData(User.GetId(), id);
                if (null == data)
                {
                    return NotFound();
                }
                return Ok(data);
            }
            catch (DeviceAccessException e)
            {
                _logger.LogError("Invalid device data access", e);
                return Forbid();
            }
        }
    }
}
