using Digit.Abstractions.Models;
using DigitService.Hubs;
using DigitService.Service;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DigitService.Impl.Logging
{
    public class SignalRLogSubscriber : IRealtimeLogSubscriber
    {
        private readonly IHubContext<LogHub> hubContext;

        public SignalRLogSubscriber(IHubContext<LogHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        public async Task Add(LogEntry entry)
        {
            if (null == entry.UserId)
            {
                return;
            }
            await hubContext.Clients.User(entry.UserId).SendAsync("log", entry);
        }
    }
}
