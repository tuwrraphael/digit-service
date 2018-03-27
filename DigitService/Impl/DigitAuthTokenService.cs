using DigitService.Models;
using DigitService.Service;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace DigitService.Impl
{
    public class DigitAuthTokenService : IDigitAuthTokenService
    {
        private readonly IMemoryCache memoryCache;
        private SemaphoreSlim semaphore;
        private readonly DigitServiceOptions options;
        private const string TokenCacheKey = "TokenCacheKey";
        private const string Scopes = "calendar.service";

        public DigitAuthTokenService(IMemoryCache memoryCache,
            IOptions<DigitServiceOptions> optionsAccessor)
        {
            this.memoryCache = memoryCache;
            semaphore = new SemaphoreSlim(1);
            options = optionsAccessor.Value;
        }

        public async Task<string> GetTokenAsync()
        {
            await semaphore.WaitAsync();
            string t = null;
            if (memoryCache.TryGetValue(TokenCacheKey, out CachedToken token))
            {
                if (token.Expires > DateTime.Now)
                {
                    t = token.Token;
                }
            }
            if (null == t)
            {
                var client = new HttpClient();
                var res = await client.PostAsync($"{options.ServiceIdentityUrl}/connect/token",
                    new FormUrlEncodedContent(new Dictionary<string, string>() {
                        { "client_id", options.DigitClientId },
                    { "client_secret", options.DigitClientSecret },
                    { "grant_type", "client_credentials" },
                    { "scope", Scopes },
                    }));
                if (!res.IsSuccessStatusCode)
                {
                    throw new AuthenticationException($"Could not retrieve access token: {res.StatusCode}");
                }
                else
                {
                    var tokens = JsonConvert.DeserializeObject<TokenResponse>(await res.Content.ReadAsStringAsync());
                    memoryCache.Set(TokenCacheKey, new CachedToken()
                    {
                        Expires = DateTime.Now.AddSeconds(tokens.expires_in),
                        Token = tokens.access_token
                    });
                    t = tokens.access_token;
                }
            }
            semaphore.Release();
            return t;
        }

        private class CachedToken
        {
            public string Token { get; set; }
            public DateTime Expires { get; set; }
        }
    }
}
