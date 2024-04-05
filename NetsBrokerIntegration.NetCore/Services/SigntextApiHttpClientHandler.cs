using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http.Headers;
using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using NetsBrokerIntegration.NetCore.Models;
using System.Net.Http.Json;

namespace NetsBrokerIntegration.NetCore.Services
{
    public class SigntextApiHttpClientHandler : DelegatingHandler
    {
        private readonly IMemoryCache memoryCache;
        private readonly HttpClient httpClient;
        private readonly IOptions<SigntextApiConfiguration> options;
        private const string cacheKeySigntextApiBearerToken = "signtext_api_bearer_token";
        public SigntextApiHttpClientHandler(IMemoryCache memoryCache, HttpClient httpClient, IOptions<SigntextApiConfiguration> options)
        {
            this.memoryCache = memoryCache;
            this.httpClient = httpClient;
            this.options = options;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = await GetBearerToken();

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<AuthenticationHeaderValue> GetBearerToken()
        {
            if(memoryCache.TryGetValue(cacheKeySigntextApiBearerToken, out var token))
            {
                var stringToken = (string)token;
                return new AuthenticationHeaderValue("Bearer", stringToken);
            }

            var formContent = new FormUrlEncodedContent(new[]
                                    {
                                        new KeyValuePair<string, string>("grant_type", "client_credentials"),
                                        new KeyValuePair<string, string>("scope", "signtext_api"),
                                        new KeyValuePair<string, string>("client_id", options.Value.ApiClientId),
                                        new KeyValuePair<string, string>("client_secret", options.Value.ApiClientSecret),
                                    });

            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.Value.TokenEndpoint))
            {
                Content = formContent
            };

            var response = await httpClient.SendAsync(request);
            var tokenEndpointResponse = await response.Content.ReadFromJsonAsync<TokenEndpointResponse>();
            memoryCache.Set(cacheKeySigntextApiBearerToken, tokenEndpointResponse.Access_token, DateTimeOffset.UtcNow.AddMinutes(30));
            return new AuthenticationHeaderValue("Bearer", tokenEndpointResponse.Access_token);
        }
    }
}
