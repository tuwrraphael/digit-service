using DigitService.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Models;
using Service;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class DeviceController : Controller
    {
        private readonly IDeviceRepository deviceRepository;
        private readonly IHubContext<LogHub> context;

        public DeviceController(IDeviceRepository deviceRepository, IHubContext<LogHub> context)
        {
            this.deviceRepository = deviceRepository;
            this.context = context;
        }

        
        [HttpPost("{id}/log")]
        public async void PostLog(string id, [FromBody]LogEntry entry)
        {
            await deviceRepository.LogAsync(id, entry);
            await context.Clients.All.InvokeAsync("log", entry);
        }

        [HttpGet("{id}/log")]
        public async Task<LogEntry[]> GetLog(string id)
        {
            return await deviceRepository.GetLogAsync(id);
        }
    }
}
