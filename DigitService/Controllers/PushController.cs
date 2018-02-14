using DigitService.Hubs;
using DigitService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Models;
using Service;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class PushController : Controller
    {
        private readonly IDeviceRepository deviceRepository;
        private readonly IHubContext<LogHub> context;

        public PushController(IDeviceRepository deviceRepository, IHubContext<LogHub> context)
        {
            this.deviceRepository = deviceRepository;
            this.context = context;
        }


        [HttpPost("channel")]
        public async void Register([FromBody]PushChannelRegistration registration)
        {
            //await deviceRepository.LogAsync(id, entry);
            //await context.Clients.All.InvokeAsync("log", entry);
        }

        [HttpGet("{id}/log")]
        public async Task<LogEntry[]> GetLog(string id, int history = 20)
        {
            return await deviceRepository.GetLogAsync(id, history);
        }
    }
}
