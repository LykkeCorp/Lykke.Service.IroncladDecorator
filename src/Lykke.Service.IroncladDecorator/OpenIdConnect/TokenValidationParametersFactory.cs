using System.Collections.Generic;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.IdentityModel.Tokens;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public class TokenValidationParametersFactory : ITokenValidationParametersFactory
    {
        private readonly ValidationSettings _validationSettings;

        public TokenValidationParametersFactory(
            ValidationSettings validationSettings)
        {
            _validationSettings = validationSettings;
        }

        public TokenValidationParameters CreateTokenValidationParameters(IEnumerable<SecurityKey> keys)
        {
            return new TokenValidationParameters
            {
                ValidAudiences = _validationSettings.ValidAudiences,
                ValidIssuers = _validationSettings.ValidIssuers,
                IssuerSigningKeys = keys
            };
        }
    }
}
