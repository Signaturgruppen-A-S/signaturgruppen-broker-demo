using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using NetsBrokerIntegration.NetCore.Extensions;
using NetsBrokerIntegration.NetCore.Services;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime;

namespace NetsBrokerIntegration.NetCore
{
    public class Startup
    {
        private IWebHostEnvironment Env { get; set; }
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
            Console.WriteLine($"Authority: {Configuration.GetAuthorityUrl()}");
            Console.WriteLine($"ClientId: {Configuration.GetClientId()}");
            Console.WriteLine($"ClientSecret: {Configuration.GetClientSecret()?.Substring(0, 6) + "..." ?? "Not found!"}");

            services.Configure<SigntextApiConfiguration>(options => Configuration.GetSection("SigntextApiConfiguration").Bind(options));

            services.AddTransient<OidcEvents>();
            services.AddHttpClient<SigntextApiHttpClientHandler>();

            services.AddHttpClient<SigntextApiService>()
                .AddHttpMessageHandler<SigntextApiHttpClientHandler>();

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
                    options.Scope.Add("openid mitid nemlogin privileges");

                    //register OIDC eventtype handler, see this class for specific implementation of OIDC events like signing, encryption, transaction signing etc.
                    options.EventsType = typeof(OidcEvents);
                    
                    options.ClaimActions.MapAll();
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                });
        }
        
        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();

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
