using System;
using System.Threading.Tasks;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelService.Models.Directions;

namespace DigitService.Controllers
{
    [Route("api")]
    [ApiController]
    public class PlannerController : ControllerBase
    {
        private readonly IPlannerService _plannerService;

        public PlannerController(IPlannerService plannerService)
        {
            _plannerService = plannerService;
        }
        [HttpGet("me/plan")]
        [Authorize("User")]
        public async Task<IActionResult> GetPlan([FromQuery]DateTimeOffset from, [FromQuery]DateTimeOffset to)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            return Ok(await _plannerService.GetPlan(User.GetId(), from, to));
        }
    }
}