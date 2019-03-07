using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Lykke.Service.IroncladDecorator.Sessions;
using Lykke.Service.IroncladDecorator.Settings;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public class IroncladFacade : IIroncladFacade
    {
        private readonly IroncladSettings _ironcladSettings;
        private readonly IDiscoveryCache _discoveryCache;
        private readonly IHttpClientFactory _httpClientFactory;

        public IroncladFacade(
            IroncladSettings ironcladSettings,
            IDiscoveryCache discoveryCache,
            IHttpClientFactory httpClientFactory)
        {
            _ironcladSettings = ironcladSettings;
            _discoveryCache = discoveryCache;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<DiscoveryResponse> GetDiscoveryResponseAsync()
        {
            var discoveryResponse = await _discoveryCache.GetAsync();

            if (discoveryResponse.IsError)
            {
                _discoveryCache.Refresh();
                throw new Exception(discoveryResponse.Error);
            }

            return discoveryResponse;
        }

        public async Task<TokenResponse> RedeemAuthorizationCodeAsync(string code, string redirectUri)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var discoveryResponse = await GetDiscoveryResponseAsync();

            var tokenResponse = await httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                Code = code,
                ClientId = _ironcladSettings.AuthClient.ClientId,
                ClientSecret = _ironcladSettings.AuthClient.ClientSecret,
                RedirectUri = redirectUri
            });

            if (tokenResponse.IsError)
                throw new Exception(tokenResponse.Error);

            return tokenResponse;
        }

        public async Task<TokenData> RefreshIroncladTokensAsync(string ironcladRefreshToken)
        {
            var discoveryResponse = await GetDiscoveryResponseAsync();

            var httpClient = _httpClientFactory.CreateClient();

            var tokenResponse = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                RefreshToken = ironcladRefreshToken,
                ClientId = _ironcladSettings.AuthClient.ClientId,
                ClientSecret = _ironcladSettings.AuthClient.ClientSecret
            });

            if (tokenResponse.IsError)
                throw new Exception(discoveryResponse.Error);

            return new TokenData(tokenResponse);
        }

        public async Task<IntrospectionResponse> IntrospectTokenAsync(string bearer)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var discoveryResponse = await GetDiscoveryResponseAsync();

            var introspectionResponse = await httpClient.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = discoveryResponse.IntrospectionEndpoint,
                ClientId = _ironcladSettings.IntrospectionClient.ClientId,
                ClientSecret = _ironcladSettings.IntrospectionClient.ClientSecret,
                Token = bearer
            });

            return introspectionResponse;
        }

        public async Task<TokenRevocationResponse> RevokeTokenAsync(string tokenTypeHint, string token)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var discoveryResponse = await GetDiscoveryResponseAsync();

            var tokenRevocationResponse = await httpClient.RevokeTokenAsync(new TokenRevocationRequest
            {
                Address = discoveryResponse.RevocationEndpoint,
                ClientId = _ironcladSettings.AuthClient.ClientId,
                ClientSecret = _ironcladSettings.AuthClient.ClientSecret,
                Token = token,
                TokenTypeHint = tokenTypeHint
            });

            return tokenRevocationResponse;
        }

        public async Task<HttpResponseMessage> GetJwks()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var discoveryResponse = await GetDiscoveryResponseAsync();

            var response = await httpClient.GetAsync(discoveryResponse.JwksUri);

            return response;
        }
    }
}
