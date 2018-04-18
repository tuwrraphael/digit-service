using DigitService.Models;
using DigitService.Service;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DigitService.Impl
{
    public class NotificationHubPushService : IPushService
    {
        private readonly NotificationHubConfig config;
        private readonly IDigitLogger logger;

        public NotificationHubPushService(IOptions<NotificationHubConfig> configAccessor, IDigitLogger logger)
        {
            config = configAccessor.Value;
            this.logger = logger;
        }

        private static string GetSASToken(string resourceUri, string keyName, string key)
        {
            var expiry = GetExpiry();
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));

            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
                HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
            return sasToken;
        }

        private static string GetExpiry()
        {
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Convert.ToString((int)sinceEpoch.TotalSeconds + 3600);
        }

        public class OctetStreamStringContent : StringContent
        {
            public OctetStreamStringContent(string content, Encoding encoding, string contentType)
                : base(content, encoding, contentType)
            {
                Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            }
        }

        public async Task Push(string user, PushPayload payload)
        {
            var pText = JsonConvert.SerializeObject(payload);
            await Push(user, pText);
        }

        public async Task RegisterUser(string user, string registrationId)
        {
            NotificationHubClient client = NotificationHubClient.CreateClientFromConnectionString(config.HubConnection, config.HubName);
            var reg = await client.GetRegistrationAsync<RegistrationDescription>(registrationId);
            reg.Tags = (new[] { user }).ToHashSet();
            await client.UpdateRegistrationAsync(reg);
        }

        public async Task Push(string user, string payload)
        {
            var uri = $"https://{config.HubNamespace}.servicebus.windows.net/{config.HubName}/messages/?api-version=2015-01";
            var token = GetSASToken(uri, config.HubSASKeyName, config.HubSASKey);
            var cl = new HttpClient();
            cl.DefaultRequestHeaders.Add("X-WNS-Type", "wns/raw");
            cl.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            cl.DefaultRequestHeaders.Add("ServiceBusNotification-Format", "windows");
            cl.DefaultRequestHeaders.Add("ServiceBusNotification-Tags", user);
            var res = await cl.PostAsync(uri, new OctetStreamStringContent(payload, Encoding.Default,
                "application/octet-stream"));
            await logger.Log(user, $"Push {payload} resulted in {res.StatusCode}");
        }
    }
}
