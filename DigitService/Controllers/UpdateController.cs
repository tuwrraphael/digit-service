using ButlerClient;
using DigitService.Hubs;
using DigitService.Models;
using DigitService.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace DigitService.Controllers
{
    [Route("api/[controller]")]
    public class UpdateController : Controller
    {
        private readonly IButler butler;
        private readonly IPushService pushService;
        private readonly CallbackOptions callBackOptions;

        public UpdateController(IButler butler, IOptions<CallbackOptions> callbackOptionsAccessor, IPushService pushService)
        {
            this.butler = butler;
            this.pushService = pushService;
            callBackOptions = callbackOptionsAccessor.Value;
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody]UpdateRequest req)
        {
            await pushService.Push(req.UserId, new PushPayload() { Action = "Test" });
            //await butler.InstallAsync(new WebhookRequest()
            //{
            //    Url = new Uri(new Uri(callBackOptions.CallbackEndpoint), "/api/update").ToString(),
            //    Data = new UpdateRequest()
            //    {
            //        UserId = req.UserId
            //    },
            //    When = DateTime.Now.AddHours(1)
            //});
            return Ok();
        }
    }
}
