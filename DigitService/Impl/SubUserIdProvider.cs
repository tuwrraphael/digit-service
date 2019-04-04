using Microsoft.AspNetCore.SignalR;

namespace DigitService.Impl
{
    public class SubUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.GetId();
        }
    }
}
