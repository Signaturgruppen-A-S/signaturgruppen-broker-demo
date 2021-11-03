using System;
using System.Linq;
using System.Security.Claims;

namespace NetsBrokerIntegration.NetCore.Extensions
{
    public static class UserExtensions
    {
        public static string GetLoaClaimValue(this ClaimsPrincipal user)
        {
            var loaClaim = user.Claims.SingleOrDefault(c => c.Type == "loa")?.Value;
            return string.IsNullOrEmpty(loaClaim)
                ? null
                : loaClaim;
        }

        public static bool CanStepUp(this ClaimsPrincipal user)
        {
            var loaClaim = GetLoaClaimValue(user);
            return loaClaim == LoaConstants.MitIdLow;
        }

        public static string GetStepUpTwoFactorClaim(this ClaimsPrincipal user)
        {
            return GetLoaClaimValue(user) switch
            {
                LoaConstants.MitIdLow => LoaConstants.MitIdSubstantial,
                _ => throw new NotSupportedException("Loa claim value is not supported")
            };
        }
    }
}
