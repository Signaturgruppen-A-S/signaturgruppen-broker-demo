using Microsoft.Extensions.Configuration;

namespace NetsBrokerIntegration.NetCore.Extensions
{
    public static class ConfigurationExtensions
    {
        public static string GetDiscoveryUrl(this IConfiguration configuration)
        {
            return configuration.GetAuthorityUrl() + "/.well-known/openid-configuration";
        }

        public static string GetAuthorityUrl(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("AppSettings:Authority");
        }

        public static string GetClientId(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("AppSettings:ClientId");
        }

        public static string GetClientSecret(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("AppSettings:ClientSecret");
        }
        public static bool SignOrEncryptRequest(this IConfiguration configuration)
        {
            return configuration.SignRequest() || configuration.EncryptRequest();
        }

        public static bool SignRequest(this IConfiguration configuration)
        {
            return configuration.GetValue<bool>("AppSettings:SignRequest");
        }

        public static bool EncryptRequest(this IConfiguration configuration)
        {
            return configuration.GetValue<bool>("AppSettings:EncryptRequest");
        }
    }
}
