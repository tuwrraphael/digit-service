using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class PushController : Controller
    {
        private readonly IUserRepository userRepository;

        public PushController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        [Authorize("User")]
        [HttpPost()]
        public async Task<IActionResult> Register([FromBody]PushChannelRegistration registration)
        {
            var id = User.Claims.Where(v => v.Type == "sub").Single().Value;
            await userRepository.RegisterPushChannel(id, registration.Uri);
            return Ok();
        }
    }
}
