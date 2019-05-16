using Digit.Focus.Model;
using Digit.Focus.Models;
using Digit.Focus.Service;
using DigitService.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class SignalRFocusSubscriber : IFocusSubscriber
    {
        private readonly IHubContext<FocusHub> hubContext;

        public SignalRFocusSubscriber(IHubContext<FocusHub> hubContext)
        {
            this.hubContext = hubContext;
        }

        public Task ActiveItemChanged(string userId, FocusItemWithExternalData currentItem)
        {
            return Task.CompletedTask;
        }

        public async Task ActiveItemsChanged(string userId, FocusManageResult res)
        {
            await hubContext.Clients.User(userId).SendAsync("focusChanged", res.ActiveItems);
        }
    }
}