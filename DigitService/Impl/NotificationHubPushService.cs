using DigitPushService.Client;
using System.Linq;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public static class DigitPushServiceClientExtension
    {
        public static async Task<bool> HasPushChannelTypeAsync(this IDigitPushServiceClient digitPushServiceClient, string userId, string channelType)
        {
            var channels = await digitPushServiceClient[userId].PushChannels.GetAllAsync();
            if (channels.Any(v => v.Options.ContainsKey(channelType)))
            {
                return true;
            }
            return false;
        }
    }
}
