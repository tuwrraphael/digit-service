using DigitService.Impl;
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
        private readonly IDigitLogger logger;

        public LocationController(ILocationService locationService, IDigitLogger logger)
        {
            this.locationService = locationService;
            this.logger = logger;
        }


        [Authorize("User")]
        [HttpGet("me/location")]
        public async Task<IActionResult> Get()
        {
            return await Get(User.GetId());
        }

        [Authorize("Service")]
        [HttpGet("{userId}/location")]
        public async Task<IActionResult> Get(string userId)
        {
            try
            {
                var loc = await locationService.GetCurrentLocation(userId);
                if (null != loc)
                {
                    return Ok(loc);
                }
                return NotFound();
            }
            catch (TimeoutException)
            {
                return NotFound();
            }
            catch (UserConfigurationException)
            {
                return NotFound();
            }
        }

        [Authorize("User")]
        [HttpPost("me/location")]
        public async Task<IActionResult> Post([FromBody]Location location)
        {
            await logger.Log(User.GetId(), "Got location");
            await locationService.LocationCallback(User.GetId(), location);
            return Ok();
        }

        [Authorize("User")]
        [HttpPut("me/location/error")]
        public async Task<IActionResult> Put([FromBody] LocationConfigurationError error)
        {
            await locationService.LocationConfigurationErrorCallback(User.GetId(), error);
            return Ok();
        }
    }
}
