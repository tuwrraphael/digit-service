using System.Collections.Generic;
using System.Threading.Tasks;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitService.Controllers
{
    [Route("api")]
    public class FocusController : Controller
    {
        private readonly IFocusStore focusStore;

        public FocusController(IFocusStore focusStore)
        {
            this.focusStore = focusStore;
        }

        [Authorize("User")]
        [HttpGet("me/focus")]
        public async Task<IActionResult> Get()
        {
            var active = await focusStore.GetActiveAsync(User.GetId());
            return Ok(active);
        }

    }
}
