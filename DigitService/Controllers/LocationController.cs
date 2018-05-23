using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/")]
    public class LocationController : Controller
    {
        private readonly ILocationService locationService;

        public LocationController(ILocationService locationService)
        {
            this.locationService = locationService;
        }


        [Authorize("User")]
        [HttpGet("me/location")]
        public async Task<IActionResult> Get()
        {
            return await Get(User.GetId());
        }

        [Authorize("Service")]
        [HttpGet("{userId}/location")]
        private async Task<IActionResult> Get(string userId)
        {
            try
            {
                return Ok(await locationService.GetCurrentLocation(userId));
            }
            catch (TimeoutException)
            {
                return NotFound();
            }
        }

        [Authorize("User")]
        [HttpPost("me/location")]
        public async Task<IActionResult> Post([FromBody]Location location)
        {
            await locationService.LocationCallback(User.GetId(), location);
            return Ok();
        }
    }
}
