using System;
using System.IdentityModel.Tokens.Jwt;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public class TokenResponseContext
    {
        private readonly OpenIdConnectMessage _openIdConnectMessage;

        private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler =
            new JwtSecurityTokenHandler();

        private readonly OpenIdConnectProtocolValidator _connectProtocolValidator =
            new OpenIdConnectProtocolValidator
            {
                RequireTimeStampInNonce = false
            };

        public TokenResponseContext(TokenResponse tokenResponse)
        {
            _openIdConnectMessage = new OpenIdConnectMessage(tokenResponse.Json);
        }

        public void Validate(
            AuthorizationRequestContext authorizationRequestContext,
            TokenValidationParameters parameters)
        {
            var context = authorizationRequestContext.GetValidationContext();

            context.ProtocolMessage = _openIdConnectMessage;

            var idToken = _openIdConnectMessage.IdToken;

            if (!_jwtSecurityTokenHandler.CanReadToken(idToken))
            {
                throw new Exception("Unable to read id token");
            }

            _jwtSecurityTokenHandler.ValidateToken(idToken, parameters, out var securityToken);

            if (!(securityToken is JwtSecurityToken validatedIdToken))
            {
                throw new Exception("Validated token is null");
            }

            context.ValidatedIdToken = validatedIdToken;

            _connectProtocolValidator.RequireNonce = false;
            _connectProtocolValidator.ValidateTokenResponse(context);
        }
    }
}
