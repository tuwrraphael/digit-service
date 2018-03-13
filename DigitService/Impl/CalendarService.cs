using DigitService.Models;
using DigitService.Service;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class CalendarService : ICalendarService
    {
        private readonly DigitServiceOptions options;
        private readonly IDigitAuthTokenService digitAuthTokenService;

        public CalendarService(IOptions<DigitServiceOptions> optionsAccessor,
            IDigitAuthTokenService digitAuthTokenService)
        {
            options = optionsAccessor.Value;
            this.digitAuthTokenService = digitAuthTokenService;
        }

        private async Task<HttpClient> GetClientAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await digitAuthTokenService.GetTokenAsync());
            return client;
        }

        public async Task<ReminderRegistration> RegisterReminder(string userId, uint minutes)
        {
            var res = await (await GetClientAsync()).PostAsync($"{options.CalendarServiceUrl}/api/users/{userId}/reminders",
                new StringContent(
                    JsonConvert.SerializeObject(new ReminderRequest()
                    {
                        ClientState = Guid.NewGuid().ToString(),
                        Minutes = minutes,
                        NotificationUri = options.ReminderCallbackUri
                    }), Encoding.UTF8, "application/json"));
            if (res.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<ReminderRegistration>(await res.Content.ReadAsStringAsync());
            }
            throw new ReminderException($"Could not register reminder: {res.StatusCode}.");
        }

        public async Task<bool> ReminderAliveAsync(string userId, string reminderId)
        {
            var res = await (await GetClientAsync()).GetAsync($"{options.CalendarServiceUrl}/api/users/{userId}/reminders/{reminderId}");
            if (res.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<bool>(await res.Content.ReadAsStringAsync());
            }
            throw new ReminderException($"Could retrieve reminder status: {res.StatusCode}.");
        }

        public async Task RenewReminder(string userId, RenewReminderRequest req)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"),
                $"{options.CalendarServiceUrl}/api/users/{userId}/reminders/{req.ReminderId}");
            var res = await (await GetClientAsync()).SendAsync(request);
            if (!res.IsSuccessStatusCode)
            {
                throw new ReminderException($"Could not renew reminder: {res.StatusCode}.");
            }
        }
    }
}
