using Microsoft.IdentityModel.Tokens;

namespace NetsBrokerIntegration.NetCore.Crypto
{
    public class RsaCryptoProvider : ICryptoProvider
    {
        public bool IsSupportedAlgorithm(string algorithm, params object[] args)
        {
            if (algorithm == "RSA-OAEP-256")
                return true;

            return false;
        }

        public object Create(string algorithm, params object[] args)
        {
            if (algorithm.Equals("RSA-OAEP-256"))
                return new RsaOaepKeyWrapProvider(args[0] as SecurityKey);

            return null;
        }

        public void Release(object cryptoInstance)
        {
        }
    }
}
