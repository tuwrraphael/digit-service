using Digit.DeviceSynchronization.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api")]
    public class SyncController : Controller
    {
        private readonly IPushSyncService _pushSyncService;

        public SyncController(IPushSyncService pushSyncService)
        {
            _pushSyncService = pushSyncService;
        }

        [HttpGet("me/sync")]
        [Authorize("User")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _pushSyncService.GetPendingSyncActions(User.GetId(), DateTimeOffset.Now));
        }

        [HttpPut("me/sync/{id}")]
        [Authorize("User")]
        public async Task<IActionResult> Done(string id)
        {
            await _pushSyncService.SetDone(User.GetId(), id);
            return Ok();
        }
    }
}
