using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DigitService.Hubs
{
    [Authorize("User")]
    public class FocusHub : Hub
    {

    }
}
