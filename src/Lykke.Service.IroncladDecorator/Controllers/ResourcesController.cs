using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.Sessions;
using Lykke.Service.IroncladDecorator.Settings;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    public class ResourcesController : ControllerBase
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly IDiscoveryCache _discoveryCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IroncladSettings _ironcladSettings;
        private readonly LifetimeSettings _lifetimeSettings;
        private readonly ILykkeSessionManager _lykkeSessionManager;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IUserSessionRepository _userSessionRepository;

        public ResourcesController(
            IUserSessionManager userSessionManager,
            IUserSessionRepository userSessionRepository,
            IClientAccountClient clientAccountClient,
            IHttpClientFactory httpClientFactory,
            IDiscoveryCache discoveryCache,
            IClientSessionsClient clientSessionsClient,
            IroncladSettings ironcladSettings,
            LifetimeSettings lifetimeSettings,
            ILykkeSessionManager lykkeSessionManager)
        {
            _ironcladSettings = ironcladSettings;
            _lifetimeSettings = lifetimeSettings;
            _lykkeSessionManager = lykkeSessionManager;
            _userSessionRepository = userSessionRepository;
            _clientAccountClient = clientAccountClient;
            _userSessionManager = userSessionManager;
            _httpClientFactory = httpClientFactory;
            _discoveryCache = discoveryCache;
            _clientSessionsClient = clientSessionsClient;
        }

        [HttpGet("~/getlykkewallettoken")]
        public async Task<IActionResult> GetLykkewalletToken()
        {
            var bearerToken = HttpContext.GetBearerTokenFromAuthorizationHeader();

            if (bearerToken == null)
                return Unauthorized();

            var httpClient = _httpClientFactory.CreateClient();

            var introspectionResponse = await IntrospectToken(httpClient, bearerToken);

            if (!introspectionResponse.IsActive)
                return Unauthorized();

            var userId = introspectionResponse.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject)?.Value;

            var authResult = await _clientSessionsClient.Authenticate(userId, "hobbit");

            var clientAccount = await _clientAccountClient.GetByIdAsync(userId);

            return new JsonResult(new
            {
                token = authResult.SessionToken,
                authResult.AuthId,
                notificationsId = clientAccount.NotificationsId
            });
        }

        [HttpGet("~/getlykkewallettokenmobile")]
        public async Task<IActionResult> GetLykkeWalletTokenMobile()
        {
            var sessionId = _userSessionManager.GetIdFromCookie();

            return await GetTokenBasedOnCookie(sessionId);
        }

        
        [HttpGet("~/token/kyc")]
        public async Task<IActionResult> GetKycToken()
        {
            var oldLykkeToken = HttpContext.GetBearerTokenFromAuthorizationHeader();

            if (string.IsNullOrWhiteSpace(oldLykkeToken))
            {
                return Unauthorized();
            }

            var lykkeSession = await _lykkeSessionManager.GetActiveAsync(oldLykkeToken);
            
            if (lykkeSession == null)
            {
                return Unauthorized();
            }

            var tokens = lykkeSession.IroncladTokens;

            if (tokens == null)
            {
                return Unauthorized();
            }

            if (IsExpired(tokens.ExpiresAt)) 
                tokens = await RefreshIroncladTokens(tokens.RefreshToken);

            lykkeSession.IroncladTokens = tokens;

            var newLykkeSession = new LykkeSession(oldLykkeToken, tokens);

            await _lykkeSessionManager.SetAsync(newLykkeSession);

            return new JsonResult(new {token = tokens.AccessToken});
        }

        private async Task<IntrospectionResponse> IntrospectToken(HttpClient httpClient, string bearer)
        {
            var discoveryResponse = await _discoveryCache.GetAsync();

            if (discoveryResponse.IsError)
            {
                _discoveryCache.Refresh();
                throw new Exception(discoveryResponse.Error);
            }

            var introspectionResponse = await httpClient.IntrospectTokenAsync(new TokenIntrospectionRequest
            {
                Address = discoveryResponse.IntrospectionEndpoint,
                ClientId = _ironcladSettings.IntrospectionClient.ClientId,
                ClientSecret =_ironcladSettings.IntrospectionClient.ClientSecret,
                Token = bearer
            });
            return introspectionResponse;
        }

        private async Task<IActionResult> GetTokenBasedOnCookie(string sessionId)
        {
            if (sessionId == null) return Unauthorized();

            var session = await _userSessionRepository.GetAsync(sessionId);
            if (session == null) return Unauthorized();

            var oldLykkeToken = session.OldLykkeToken;
            if (oldLykkeToken == null) return Unauthorized();

            var lykkeClientId = session.LykkeClientId;

            var clientAccount = await _clientAccountClient.GetByIdAsync(lykkeClientId);

            var authId = session.AuthId;

            return new JsonResult(new
            {
                token = oldLykkeToken,
                authId = authId,
                notificationsId = clientAccount.NotificationsId
            });
        }

        private bool IsExpired(DateTimeOffset expiresAt)
        {
            return expiresAt.Subtract(_lifetimeSettings.IroncladAccessTokenTimeBeforeRefresh) <= DateTimeOffset.UtcNow;
        }

        private async Task<TokenData> RefreshIroncladTokens(string ironcladRefreshToken)
        {
            var discoveryResponse = await _discoveryCache.GetAsync();

            if (discoveryResponse.IsError)
            {
                _discoveryCache.Refresh();
                throw new Exception(discoveryResponse.Error);
            }
            
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
    }
}
