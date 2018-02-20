using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class DeviceController : Controller
    {
        private readonly IDeviceRepository deviceRepository;

        public DeviceController(IDeviceRepository deviceRepository)
        {
            this.deviceRepository = deviceRepository;
        }

        
        [HttpPost("{id}/log")]
        public async void PostLog(string id, [FromBody]LogEntry entry)
        {
            await deviceRepository.LogAsync(id, entry);
        }

        [HttpGet("{id}/log")]
        public async Task<LogEntry[]> GetLog(string id, int history = 20)
        {
            return await deviceRepository.GetLogAsync(id, history);
        }
    }
}
