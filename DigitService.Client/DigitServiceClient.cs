using DigitService.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OAuthApiClient.Abstractions;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DigitService.Client
{

    public class DigitServiceClient : IDigitServiceClient
    {
        private readonly DigitServiceOptions options;
        private readonly IAuthenticationProvider authenticationProvider;

        public DigitServiceClient(IAuthenticationProvider authenticationProvider, IOptions<DigitServiceOptions> optionsAccessor)
        {
            options = optionsAccessor.Value;
            this.authenticationProvider = authenticationProvider;
        }

        private async Task<HttpClient> ClientFactory()
        {
            var client = new HttpClient
            {
                BaseAddress = options.DigitServiceBaseUri
            };
            await authenticationProvider.AuthenticateClient(client);
            return client;
        }

        private async Task<HttpClient> ClientFactoryUnauthorized()
        {
            var client = new HttpClient
            {
                BaseAddress = options.DigitServiceBaseUri
            };
            return client;
        }

        public IDeviceCollection Device => new DeviceCollection(ClientFactory);

        private class DeviceCollection : IDeviceCollection
        {
            private readonly Func<Task<HttpClient>> clientFactory;

            public DeviceCollection(Func<Task<HttpClient>> clientFactory)
            {
                this.clientFactory = clientFactory;
            }

            public IDevice this[string deviceId] => new DeviceClient(deviceId, clientFactory);
        }

        private class DeviceClient : IDevice
        {
            private readonly string deviceId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public DeviceClient(string deviceId, Func<Task<HttpClient>> clientFactory)
            {
                this.deviceId = deviceId;
                this.clientFactory = clientFactory;
            }

            public IBattery Battery => new BatteryClient(deviceId, clientFactory);

            public async Task<bool> Claim()
            {
                var client = await clientFactory();
                HttpResponseMessage res = await client.PostAsync($"api/device/{deviceId}/claim",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
                if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new DigitServiceException("Not authorized to claim device");
                }
                if (!res.IsSuccessStatusCode)
                {
                    throw new DigitServiceException($"Claim device error: {res.StatusCode}");
                }
                return true;
            }
            private class BatteryClient : IBattery
            {
                private readonly string deviceId;
                private readonly Func<Task<HttpClient>> clientFactory;

                public BatteryClient(string deviceId, Func<Task<HttpClient>> clientFactory)
                {
                    this.deviceId = deviceId;
                    this.clientFactory = clientFactory;
                }

                public async Task AddMeasurement(BatteryMeasurement measurement)
                {
                    var client = await clientFactory();
                    var json = JsonConvert.SerializeObject(measurement);
                    HttpResponseMessage res = await client.PostAsync($"api/device/{deviceId}/battery",
                        new StringContent(json, Encoding.UTF8, "application/json"));
                    if (!res.IsSuccessStatusCode)
                    {
                        throw new DigitServiceException($"Battery measurement post error {res.StatusCode}");
                    }
                }
            }
        }

        public ILocation Location => new LocationClient("me", ClientFactory);

        private class LocationClient : ILocation
        {
            private readonly string userId;
            private readonly Func<Task<HttpClient>> clientFactory;

            public LocationClient(string userId, Func<Task<HttpClient>> clientFactory)
            {
                this.userId = userId;
                this.clientFactory = clientFactory;
            }

            public ILocation this[string userId] => new LocationClient(userId, clientFactory);

            public async Task AddLocation(Location location)
            {
                var client = await clientFactory();
                var json = JsonConvert.SerializeObject(location);
                HttpResponseMessage res = await client.PostAsync($"api/{userId}/location",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                if (!res.IsSuccessStatusCode)
                {
                    throw new DigitServiceException($"Location post error {res.StatusCode}");
                }
            }
        }

        public async Task<bool> LogAsync(string message, int code = 0, DateTime? occurenceTime = null)
        {
            var json = JsonConvert.SerializeObject(new LogEntry()
            {
                Code = code,
                Message = message,
                OccurenceTime = occurenceTime ?? DateTime.Now,
                Author = options.LogAuthor
            });
            var client = await ClientFactoryUnauthorized();
            HttpResponseMessage res = await client.PostAsync($"api/device/12345/log",
                new StringContent(json, Encoding.UTF8, "application/json"));
            return res.IsSuccessStatusCode;
        }

        public async Task SetupPushChannel(PushChannelRegistration channelRegistration)
        {
            var json = JsonConvert.SerializeObject(channelRegistration);
            var client = await ClientFactory();
            HttpResponseMessage res = await client.PostAsync($"api/push",
                 new StringContent(json, Encoding.UTF8, "application/json"));
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new DigitServiceException("Not authorized to register push channel");
            }
            if (!res.IsSuccessStatusCode)
            {
                throw new DigitServiceException($"Push channel registration error {res.StatusCode}");
            }
        }
    }
}
