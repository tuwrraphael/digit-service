#if NETSTANDARD2_0
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OAuthApiClient.Abstractions;
using System;

namespace DigitService.Client
{
    public static class DigitServiceClientServiceCollectionExtension
    {
        public static void AddDigitServiceClient(this IServiceCollection services,
            Uri baseUri,
            IAuthenticationProviderBuilder authenticationProviderBuilder)
        {
            services.AddDigitServiceClient(new DigitServiceOptions()
            {
                DigitServiceBaseUri = baseUri
            }, authenticationProviderBuilder);
        }

        public static void AddDigitServiceClient(this IServiceCollection services,
            DigitServiceOptions options,
            IAuthenticationProviderBuilder authenticationProviderBuilder)
        {
            var factory = authenticationProviderBuilder.GetFactory();
            services.Configure<DigitServiceOptions>(v =>
            {
                v.DigitServiceBaseUri = options.DigitServiceBaseUri;
                v.LogAuthor = options.LogAuthor;
            });
            services.AddTransient<IDigitServiceClient>(v => new DigitServiceClient(factory(v), v.GetService<IOptions<DigitServiceOptions>>()));
        }
    }
}
#endif