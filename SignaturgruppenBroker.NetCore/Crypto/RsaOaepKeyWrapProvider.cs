using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;

namespace NetsBrokerIntegration.NetCore.Crypto
{
    public class RsaOaepKeyWrapProvider : KeyWrapProvider
    {
        private readonly X509SecurityKey rsaKey;
        public RsaOaepKeyWrapProvider(SecurityKey key)
        {
            rsaKey = key as X509SecurityKey;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override byte[] UnwrapKey(byte[] keyBytes)
        {
            throw new NotImplementedException();
        }

        public override byte[] WrapKey(byte[] data)
        {
            using var rsa = rsaKey.PublicKey as RSA;
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }

        public override string Algorithm { get; }
        public override string Context { get; set; }
        public override SecurityKey Key { get; }
    }
}
