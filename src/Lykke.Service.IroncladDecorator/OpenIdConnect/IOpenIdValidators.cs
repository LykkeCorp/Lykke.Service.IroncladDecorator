using System.Threading.Tasks;
using IdentityModel.Client;
using Lykke.Service.IroncladDecorator.Sessions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public interface IOpenIdValidators
    {
        Task<IdTokenValidationResult> ValidateWebClientTokenResponseAsync(UserSession userSession, TokenResponse tokenResponse);

        void ValidateWebClientAuthenticationResponse(UserSession userSession, OpenIdConnectMessage authenticationResponse);
    }
}
