using DigitService.Models;
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
        private readonly IUserRepository userRepository;

        public UserController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }


        [HttpPost()]
        public async Task<IActionResult> Register()
        {
            await userRepository.CreateUser(new NewUser() { Id = User.GetId() });
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (await userRepository.Exists(User.GetId()))
            {
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
