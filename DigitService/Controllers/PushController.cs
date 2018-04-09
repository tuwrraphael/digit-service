using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class PushController : Controller
    {
        private readonly IPushService pushService;
        private readonly IUserService userService;

        public PushController(IPushService pushService, IUserService userService)
        {
            this.pushService = pushService;
            this.userService = userService;
        }

        [Authorize("User")]
        [HttpPost()]
        public async Task<IActionResult> Register([FromBody]PushChannelRegistration registration)
        {
            await pushService.RegisterUser(User.GetId(), registration.Uri);
            await userService.RegisterPushChannel(User.GetId(), registration.Uri);
            return Ok();
        }
    }
}
