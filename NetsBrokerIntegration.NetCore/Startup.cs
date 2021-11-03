using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using NetsBrokerIntegration.NetCore.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetsBrokerIntegration.NetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddLogging(opt =>
            {
                opt.ClearProviders();
                opt.AddConsole();
            });

            var mvcBuilder = services.AddControllersWithViews();

            if (Env.IsDevelopment())
            {
                mvcBuilder.AddRazorRuntimeCompilation();
                IdentityModelEventSource.ShowPII = true;
            }

            Console.WriteLine("Start: Setting up OIDC");
            Console.WriteLine($"Authority: {Configuration.GetValue<string>("AppSettings:Authority")}");
            Console.WriteLine($"ClientId: {Configuration.GetValue<string>("AppSettings:ClientId")}");
            Console.WriteLine($"ClientSecret: {Configuration.GetValue<string>("AppSettings:ClientSecret")?.Substring(0, 10) + "..." ?? "Not found!"}");
            Console.WriteLine($"ClientSigningKey: {Configuration.GetValue<string>("AppSettings:ClientSigningKey")?.Substring(0, 10) + "..." ?? "Not found!"}");

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies", opt =>
                {
                    opt.ReturnUrlParameter = "/Secure/Claims";
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.SignInScheme = "Cookies";
                    options.Authority = Configuration.GetValue<string>("AppSettings:Authority");
                    options.ClientId = Configuration.GetValue<string>("AppSettings:ClientId");
                    options.ClientSecret = Configuration.GetValue<string>("AppSettings:ClientSecret");
                    options.ResponseType = "code";
                    options.RequireHttpsMetadata = true;
                    options.CallbackPath = "/signin-oidc";
                    options.SignedOutRedirectUri = "/signout-oidc";
                    options.Scope.Clear();
                    options.Scope.Add("openid nemid mitid");
                    options.Events.OnRemoteFailure = context =>
                    {
                        var queryParams = context.Request.QueryString.ToString();
                        if (context.Request.Method == "POST")
                        {
                            queryParams = QueryHelpers.AddQueryString("", context.Request.Form?.ToDictionary(p => p.Key, p => p.Value.ToString()));
                        }

                        context.Response.Redirect("/Home/Error" + queryParams);
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };
                    options.Events.OnTokenResponseReceived = context =>
                    {
                        AddTokenToAttributes(context, "transaction_token");
                        AddTokenToAttributes(context, "userinfo_token");
                        return Task.CompletedTask;
                    };
                    options.ClaimActions.MapAll();
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                });
        }

        private static void AddTokenToAttributes(TokenResponseReceivedContext context, string tokenName)
        {
            if (context.TokenEndpointResponse.Parameters.ContainsKey(tokenName))
            {
                var transactionToken = context.TokenEndpointResponse.Parameters[tokenName];
                context.Properties.Items[$".Token.{tokenName}"] = transactionToken;
            }
        }

        private async Task SetSignedRequestObject(OpenIdConnectOptions options, RedirectContext context)
        {
            var claims = new List<Claim>();
            foreach (var property in context.Properties.Parameters)
            {
                claims.Add(new Claim(property.Key, property.Value.ToString()));
            }
            foreach (var param in context.ProtocolMessage.Parameters)
            {
                claims.Add(new Claim(param.Key, param.Value.ToString()));
            }

            claims.Remove(claims.Single(x => x.Type == "scope"));
            context.ProtocolMessage.RemoveParameter("scope");

            var scopeString = "openid";
            if (context.Request.Query.ContainsKey("scope"))
            {
                scopeString += " " + context.Request.Query["scope"];
            }

            if (context.Request.Query.ContainsKey("nemid_pid"))
            {
                scopeString += " nemid.pid";
            }

            claims.Add(new Claim("scope", scopeString));

            if (bool.TryParse(context.Request.Query["enable_step_up"], out bool parsedResult) && parsedResult)
            {
                claims.Add(new Claim("prompt", "login"));
            }

            if (context.Request.Query.ContainsKey("acr"))
            {
                var acrvalue = context.Request.Query["acr"];
                claims.Add(new Claim("acr_values", acrvalue));
            }

            var idpValues = BuildIdpValues(context);
            claims.Add(new Claim("idp_values", idpValues));
            var idpParameters = BuildIdpParameters(context);
            claims.Add(new Claim("idp_params", idpParameters));

            if (context.Request.Query.ContainsKey("language"))
            {
                var language = context.Request.Query["language"];
                claims.Add(new Claim("language", CultureInfo.GetCultureInfo(language).TwoLetterISOLanguageName));
            }

            var now = DateTime.UtcNow;
            var securityTokenDesciptor = new SecurityTokenDescriptor
            {
                Issuer = options.ClientId,
                Audience = Configuration.GetValue<string>("AppSettings:Authority"),
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(20),
                Claims = claims.ToDictionary(x => x.Type, x => (object)x.Value),
                SigningCredentials = GetSigningCredentials()
            };

            var jwtHandler = new JsonWebTokenHandler();
            string jwt = jwtHandler.CreateToken(securityTokenDesciptor);
            context.ProtocolMessage.RemoveParameter("response_type");

            if (UseRequestUri(context))
            {
                var request_uri = await GetRequestUri(context, jwt);
                context.ProtocolMessage.SetParameter("request_uri", request_uri);
            }
            else
            {
                context.ProtocolMessage.SetParameter("request", jwt);
            }
        }

        private string BuildIdpValues(RedirectContext context)
        {
            if (context.Request.Query.ContainsKey("idp_values"))
            {
                return context.Request.Query["idp_values"];
            }

            var idpValues = new List<string>();
            if (context.Request.Query["mitidEnabled"] == "true")
            {
                idpValues.Add("mitid");
            }

            if (context.Request.Query["nemidEnabled"] == "true")
            {
                idpValues.Add("nemid");
            }
            return idpValues.Any() ? idpValues.Aggregate((i, j) => i + " " + j) : "";
        }

        private async Task<string> GetRequestUri(RedirectContext context, string jwt)
        {
            var cache = context.HttpContext.RequestServices.GetService<IDistributedCache>();
            var key = Guid.NewGuid().ToString();
            var options = new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(3) };
            await cache.SetStringAsync(key, jwt, options);
            return $"{Configuration.GetValue<string>("AppSettings:ApplicationUrl")}/requestobject/{key}";
        }

        private static bool UseRequestUri(RedirectContext context)
        {
            return IsTextOrHtmlSignFlow(context, "mitid")
                || IsNemIdSignFlow(context);
        }

        private static string BuildIdpParameters(RedirectContext context)
        {
            var idpParameters = new IdpParameters();

            if (context.Request.Query.ContainsKey("nemidEnabled"))
            {
                SetupNemIdParams(context, idpParameters);
            }

            if (context.Request.Query.ContainsKey("mitidEnabled") || context.Request.Query["idp_values"] == "mitid")
            {
                SetupMitIdParams(context, idpParameters);
            }

            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true
            };
            return JsonSerializer.Serialize(idpParameters, options);
        }


        private static bool IsTextOrHtmlSignFlow(RedirectContext context, string idpName)
        {
            return context.Request.Query.ContainsKey($"{idpName}Enabled")
                && context.Request.Query.ContainsKey($"{idpName}_sign_text_type")
                && context.Request.Query.ContainsKey($"{idpName}_sign_text");
        }

        private static void SetupMitIdParams(RedirectContext context, IdpParameters idpParameters)
        {
            if (IsTextOrHtmlSignFlow(context, "mitid"))
            {
                idpParameters.MitIdParameters.TransactionTextType = context.Request.Query["mitid_sign_text_type"];
                idpParameters.MitIdParameters.TransactionText = context.Request.Query["mitid_sign_text"];
            }

            idpParameters.MitIdParameters.ReferenceText = context.Request.Query["mitid_reference_text"];
            var requirePsd2 = bool.TryParse(context.Request.Query["mitid_require_psd2"], out bool parsedPsd2Result);
            idpParameters.MitIdParameters.RequirePsd2 = requirePsd2 && parsedPsd2Result;
            idpParameters.MitIdParameters.EnableAppSwitch = true;
            idpParameters.MitIdParameters.LoaValue = context.Request.Query["mitid_loa_value"];
            var enableStepUp = bool.TryParse(context.Request.Query["enable_step_up"], out bool parsedStepUpResult);
            idpParameters.MitIdParameters.EnableStepUp = enableStepUp && parsedStepUpResult;
        }

        private static bool IsNemIdSignFlow(RedirectContext context)
        {
            return (IsTextOrHtmlSignFlow(context, "nemid") || IsPdfSignFlow());

            bool IsPdfSignFlow()
            {
                return context.Request.Query.ContainsKey($"nemidEnabled") 
                    && context.Request.Query["nemid_sign_text_type"] == "pdf";
            }
        }

        private static void SetupNemIdParams(RedirectContext context, IdpParameters idpParameters)
        {
            if (IsNemIdSignFlow(context))
            {
                var signTextType = context.Request.Query["nemid_sign_text_type"];
                idpParameters.NemIDParameters.SignTextBase64 = signTextType.ToString() switch
                {
                    "text" => context.Request.Query["nemid_sign_text"],
                    "pdf" => new NemIdFiles().DemoSignPdfBase64,
                    _ => null
                };
                idpParameters.NemIDParameters.SignTextType = signTextType;
            }
            if (context.Request.Query.ContainsKey("nemid_apptransactiontext"))
            {
                idpParameters.NemIDParameters.CodeAppTransactionTextBase64 = context.Request.Query["nemid_apptransactiontext"];
            }
            
            bool.TryParse(context.Request.Query["nemid_private_to_business"], out var enableNemIdToBusiness);
            idpParameters.NemIDParameters.PrivateNemIdToBusiness = enableNemIdToBusiness;
        }

        private SigningCredentials GetSigningCredentials()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("AppSettings:ClientSecret")));
            return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        private IWebHostEnvironment Env { get; set; }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseHttpsRedirection(); //Only use in development because production      is running in docker with a reverse proxy
            }
            else
            {
                var forwardOptions = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                    RequireHeaderSymmetry = false
                };

                forwardOptions.KnownNetworks.Clear();
                forwardOptions.KnownProxies.Clear();
                app.UseForwardedHeaders(forwardOptions);


                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseIOSCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
