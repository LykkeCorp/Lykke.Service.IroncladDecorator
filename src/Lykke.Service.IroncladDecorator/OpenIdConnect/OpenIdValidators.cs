using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Lykke.Service.IroncladDecorator.Sessions;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public class OpenIdValidators : IOpenIdValidators
    {
        private readonly ValidationSettings _validationSettings;
        private readonly IDiscoveryCache _discoveryCache;
        private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;
        private readonly OpenIdConnectProtocolValidator _connectProtocolValidator;

        public OpenIdValidators(
            ValidationSettings validationSettings,
            IDiscoveryCache discoveryCache)
        {
            _validationSettings = validationSettings;
            _discoveryCache = discoveryCache;
            _connectProtocolValidator = new OpenIdConnectProtocolValidator
            {
                RequireTimeStampInNonce = false
            };
            _jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            _jwtSecurityTokenHandler.InboundClaimTypeMap.Clear();
        }

        public async Task<IdTokenValidationResult> ValidateWebClientTokenResponseAsync(
            UserSession userSession,
            TokenResponse tokenResponse)
        {
            var discoveryResponse = await _discoveryCache.GetAsync();

            if (discoveryResponse.IsError)
            {
                _discoveryCache.Refresh();
                throw new Exception(discoveryResponse.Error);
            }

            var keys = discoveryResponse.KeySet.Keys.Select(SecurityKeySelector);

            var parameters = new TokenValidationParameters
            {
                ValidAudiences = _validationSettings.ValidAudiences,
                ValidIssuers = _validationSettings.ValidIssuers,
                IssuerSigningKeys = keys
            };

            var context = GetValidationContext(userSession);

            context.ProtocolMessage = new OpenIdConnectMessage(tokenResponse.Json);

            var idToken = context.ProtocolMessage.IdToken;

            if (!_jwtSecurityTokenHandler.CanReadToken(idToken))
            {
                throw new Exception("Unable to read id token");
            }

            var user = _jwtSecurityTokenHandler.ValidateToken(idToken, parameters, out var securityToken);

            if (!(securityToken is JwtSecurityToken validatedIdToken))
            {
                throw new Exception("Validated token is null");
            }

            context.ValidatedIdToken = validatedIdToken;

            _connectProtocolValidator.ValidateTokenResponse(context);

            return new IdTokenValidationResult
            {
                ValidatedClaimsPrincipal = user,
                ValidatedIdToken = validatedIdToken
            };
        }

        public void ValidateWebClientAuthenticationResponse(UserSession userSession, OpenIdConnectMessage authenticationResponse)
        {
            var context = GetValidationContext(userSession);

            context.ProtocolMessage = authenticationResponse;

            _connectProtocolValidator.ValidateAuthenticationResponse(context);
        }

        private OpenIdConnectProtocolValidationContext GetValidationContext(UserSession userSession)
        {
            var authorizationRequest = userSession.Get<OpenIdConnectMessage>("AuthorizationRequest");

            if (authorizationRequest == null)
            {
                throw new Exception("No AuthenticationRequest in user session");
            }

            return new OpenIdConnectProtocolValidationContext
            {
                ClientId = authorizationRequest.ClientId,
                Nonce = authorizationRequest.Nonce,
                State = authorizationRequest.State
            };
        }

        private SecurityKey SecurityKeySelector(IdentityModel.Jwk.JsonWebKey jsonWebKey)
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
