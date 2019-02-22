using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public interface ITokenValidationParametersFactory
    {
        TokenValidationParameters CreateTokenValidationParameters(IEnumerable<SecurityKey> keys);
    }
}
