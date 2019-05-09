using Digit.Abstractions.Service;
using Digit.Focus.Model;
using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TravelService.Models.Directions;

namespace DigitService.Controllers
{
    [Route("api/")]
    public class LocationController : Controller
    {
        private readonly IDigitLogger logger;
        private readonly IFocusService focusService;

        public LocationController(IDigitLogger logger, IFocusService focusService)
        {
            this.logger = logger;
            this.focusService = focusService;
        }

        [Authorize("User")]
        [HttpPost("me/location")]
        public async Task<IActionResult> Post([FromBody]Location location)
        {
            var userId = User.GetId();
            var res = await focusService.LocationUpdateReceivedAsync(userId, location);
            return Ok(res);
        }

        [Authorize("User")]
        [HttpPut("me/location/error")]
        public async Task<IActionResult> Put([FromBody] LocationConfigurationError error)
        {
            await logger.Log(User.GetId(), "Location configuration error", 3);
            return Ok();
        }
    }
}
