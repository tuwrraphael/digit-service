using System.Collections.Generic;
using System.Threading.Tasks;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitService.Controllers
{
    [Route("api")]
    public class FocusController : Controller
    {
        private readonly IFocusStore focusStore;
        private readonly IFocusService focusService;

        public FocusController(IFocusStore focusStore, IFocusService focusService)
        {
            this.focusStore = focusStore;
            this.focusService = focusService;
        }

        [Authorize("User")]
        [HttpGet("me/focus")]
        public async Task<IActionResult> Get()
        {
            var active = await focusStore.GetActiveAsync(User.GetId());
            return Ok(active);
        }

        [Authorize("User")]
        [HttpGet("me/focus/{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var item = await focusStore.Get(User.GetId(), id);
            return Ok(item);
        }

        [Authorize("User")]
        [HttpPatch("me/focus")]
        public async Task<IActionResult> Patch()
        {
            await focusService.PatchAsync(User.GetId());
            var active = await focusStore.GetActiveAsync(User.GetId());
            return Ok(active);
        }

        [Authorize("User")]
        [HttpPut("me/focus/{id}/directions/place")]
        public async Task<IActionResult> SetPlace(SetPlaceRequest putPlace)
        {
            var active = await focusStore.GetActiveAsync(User.GetId());
            return Ok(active);
        }
    }
}
