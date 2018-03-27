using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Authorize("User")]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly IUserService userService;

        public UserController(IUserService userService)
        {
            this.userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> Create()
        {
            var userInfo = await userService.CreateAsync(User.GetId());
            return Ok(userInfo);
        }

        [HttpPatch]
        public async Task<IActionResult> Maintain()
        {
            var userInfo = await userService.MaintainAsync(User.GetId());
            if (null == userInfo)
            {
                return NotFound();
            }
            return Ok(userInfo);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userInfo = await userService.GetInformationAsync(User.GetId());
            if (null != userInfo)
            {
                return Ok(userInfo);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
