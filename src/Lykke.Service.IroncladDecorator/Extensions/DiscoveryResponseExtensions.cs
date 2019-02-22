using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;

namespace Lykke.Service.IroncladDecorator.Extensions
{
    public static class DiscoveryResponseExtensions
    {
        public static IEnumerable<SecurityKey> GetSecurityKeys(this DiscoveryResponse discoveryResponse)
        {
            var keys = discoveryResponse.KeySet?.Keys?.Select(SecurityKeySelector);

            return keys;
        }

        private static SecurityKey SecurityKeySelector(IdentityModel.Jwk.JsonWebKey jsonWebKey)
        {
            var e = Base64Url.Decode(jsonWebKey.E);
            var n = Base64Url.Decode(jsonWebKey.N);
            var key = new RsaSecurityKey(new RSAParameters
            {
                Exponent = e,
                Modulus = n,
            })
            {
                KeyId = jsonWebKey.Kid
            };
            return key;
        }
    }
}
