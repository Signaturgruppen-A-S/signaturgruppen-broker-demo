using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using NetsBrokerIntegration.NetCore.Constants;
using System.Net.Http;
using NetsBrokerIntegration.NetCore.Extensions;
using NetsBrokerIntegration.NetCore.Crypto;
using System.Security.Cryptography.X509Certificates;
using NetsBrokerIntegration.NetCore.Models;
using System.Text.Json;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace NetsBrokerIntegration.NetCore.Services
{
    public class OidcEvents : OpenIdConnectEvents
    {
        private readonly IConfiguration configuration;
        private readonly SigntextApiService signtextApiService;
        private readonly IMemoryCache memoryCache;

        public OidcEvents(IConfiguration configuration, SigntextApiService signtextApiService, IMemoryCache memoryCache)
        {
            this.configuration = configuration;
            this.signtextApiService = signtextApiService;
            this.memoryCache = memoryCache;
        }
        public override async Task RedirectToIdentityProvider(RedirectContext context)
        {
            await SetCustomAuthParameters(context);
        }

        public override Task RemoteFailure(RemoteFailureContext context)
        {
            var queryParams = context.Request.QueryString.ToString();
            if (context.Request.Method == "POST")
            {
                queryParams = QueryHelpers.AddQueryString("", context.Request.Form?.ToDictionary(p => p.Key, p => p.Value.ToString()));
            }

            context.Response.Redirect("/Home/Error" + queryParams);
            context.HandleResponse();
            return Task.CompletedTask;
        }

        public override Task TokenResponseReceived(TokenResponseReceivedContext context)
        {
            AddTransactionTokenToAttributes(context);
            return Task.CompletedTask;
        }

        private async Task SetCustomAuthParameters(RedirectContext context)
        {
            context.ProtocolMessage.Parameters.Add("idp_params", BuildIdpParameters(context));

            if (context.Request.Query.ContainsKey("language"))
            {
                var language = context.Request.Query["language"];
                context.ProtocolMessage.Parameters.Add("language", CultureInfo.GetCultureInfo(language).TwoLetterISOLanguageName);
            }
            if (IsSigntextDemo(context))
            {
                await SetupSigntextIdOidcParamUsingSigntextApiIntegration(context);
            }

            if (configuration.SignOrEncryptRequest())
            {
                await SignRequest(context);
            }

            await Task.CompletedTask;

            static bool IsSigntextDemo(RedirectContext context)
            {
                return context.Request.Query.ContainsKey("signtextDemo") && bool.Parse(context.Request.Query["signtextDemo"]);
            }
        }

        private async Task SetupSigntextIdOidcParamUsingSigntextApiIntegration(RedirectContext context)
        {
            //add scope: transaction_token, to retrieve transaction_token from token endpoint
            //add prompt login to force authentication and transaction signing flow
            //retrive signtext id from signtext API
            //add signtext_id OIDC param to authentication request

            var signtextApiService = context.HttpContext.RequestServices.GetService<SigntextApiService>();
            var signtextId = await signtextApiService.UploadPdf();

            context.ProtocolMessage.Parameters["scope"] = context.ProtocolMessage.Parameters["scope"] + " transaction_token";
            context.ProtocolMessage.Parameters.Add("prompt", "login");
            context.ProtocolMessage.Parameters.Add("signtext_id", signtextId);
            context.Properties.Items["fetchPades"] = "true";
        }

        private async Task SignRequest(RedirectContext context)
        {
            var claims = new List<Claim>();
            foreach (var param in context.ProtocolMessage.Parameters)
            {
                claims.Add(new Claim(param.Key, param.Value.ToString()));
                if (param.Key == "client_id" || param.Key == "redirect_uri")
                {
                    continue;
                }
                context.ProtocolMessage.RemoveParameter(param.Key);
            }
            var now = DateTime.UtcNow;
            var securityTokenDesciptor = new SecurityTokenDescriptor
            {
                Issuer = configuration.GetValue<string>("AppSettings:ClientId"),
                Audience = configuration.GetValue<string>("AppSettings:Authority"),
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(20),
                Claims = claims.ToDictionary(x => x.Type, x => (object)x.Value),
                SigningCredentials = GetSigningCredentials()
            };

            await SetEncryption(securityTokenDesciptor, context);

            var jwtHandler = new JsonWebTokenHandler();
            string jwt = jwtHandler.CreateToken(securityTokenDesciptor);
            context.ProtocolMessage.SetParameter("request", jwt);
        }

        private async Task SetEncryption(SecurityTokenDescriptor securityTokenDesciptor, RedirectContext context)
        {
            if (configuration.EncryptRequest())
            {
                var imemCache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
                imemCache.TryGetValue(CacheConstants.EncryptionKey, out IdentityModel.Jwk.JsonWebKey enckeyJson);

                if (enckeyJson == null)
                {
                    var client = context.HttpContext.RequestServices.GetService<IHttpClientFactory>().CreateClient();
                    var discoUrl = configuration.GetDiscoveryUrl();
                    var disco = await client.GetDiscoveryDocumentAsync(discoUrl);
                    if (disco.IsError)
                    {
                        throw new Exception(disco.Error);
                    }

                    enckeyJson = disco.KeySet.Keys.FirstOrDefault(k => k.Use == "enc");
                    imemCache.Set(CacheConstants.EncryptionKey, enckeyJson, DateTimeOffset.Now.AddHours(1));
                }

                if (enckeyJson != null)
                {
                    securityTokenDesciptor.EncryptingCredentials = GetEncryptingCredentials(enckeyJson.X5c.First());
                }
            }
        }

        private EncryptingCredentials GetEncryptingCredentials(string encryptingCert)
        {
            var cert = new X509Certificate2(Convert.FromBase64String(encryptingCert));

            var encCreds = new EncryptingCredentials(new X509SecurityKey(cert), "RSA-OAEP-256", "A256CBC-HS512")
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false, CustomCryptoProvider = new RsaCryptoProvider() }
            };

            return encCreds;
        }

        private static string BuildIdpParameters(RedirectContext context)
        {
            var idpParameters = new IdpParameters();

            SetupMitIdParams(context, idpParameters);

            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true
            };
            return JsonSerializer.Serialize(idpParameters, options);
        }

        private static void SetupMitIdParams(RedirectContext context, IdpParameters idpParameters)
        {
            idpParameters.MitIdParameters.ReferenceText = context.Request.Query["mitid_reference_text"];
            idpParameters.MitIdParameters.LoaValue = context.Request.Query["mitid_loa_value"];
        }

        private async void AddTransactionTokenToAttributes(TokenResponseReceivedContext context)
        {
            if (context.TokenEndpointResponse.Parameters.ContainsKey("transaction_token"))
            {
                var transactionToken = context.TokenEndpointResponse.Parameters["transaction_token"];
                context.Properties.Items[".Token.transaction_token"] = transactionToken;

                if (context.Properties.Items.ContainsKey("fetchPades"))
                {
                    var padesB64 = await signtextApiService.GetPAdES(transactionToken);
                    memoryCache.Set("pades", padesB64);
                }
            }
        }

        private SigningCredentials GetSigningCredentials()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:ClientSecret")));
            return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }
    }
}
