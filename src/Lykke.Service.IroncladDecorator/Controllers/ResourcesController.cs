using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.IroncladDecorator.Settings;
using Lykke.Service.IroncladDecorator.UserSession;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    public class ResourcesController : ControllerBase
    {
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly IDiscoveryCache _discoveryCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ResourcesController> _logger;
        private readonly IroncladSettings _ironcladSettings;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IUserSessionRepository _userSessionRepository;

        public ResourcesController(
            IUserSessionManager userSessionManager,
            IUserSessionRepository userSessionRepository,
            IClientAccountClient clientAccountClient,
            IHttpClientFactory httpClientFactory,
            IDiscoveryCache discoveryCache,
            IClientSessionsClient clientSessionsClient,
            ILogger<ResourcesController> logger,
            IroncladSettings ironcladSettings
        )
        {
            _logger = logger;
            _ironcladSettings = ironcladSettings;
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
            //todo: extract somewhere
            HttpContext.Request.Headers.TryGetValue("Authorization", out var headers);
            var authorizationHeader = headers.ToArray()[0];
            if (authorizationHeader == null)
                return Unauthorized();
            var bearer = authorizationHeader.Split(' ')[1];
            var httpClient = _httpClientFactory.CreateClient();

            var introspectionResponse = await IntrospectToken(httpClient, bearer);

            if (!introspectionResponse.IsActive)
                return Unauthorized();

            var userId = introspectionResponse.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject)?.Value;

            var authResult = await _clientSessionsClient.Authenticate(userId, "hobbit");

            var clientAccount = await _clientAccountClient.GetByIdAsync(userId);

            return new JsonResult(new
            {
                Token = authResult.SessionToken,
                authResult.AuthId,
                clientAccount.NotificationsId
            });
        }

        [HttpGet("~/getlykkewallettokenmobile")]
        public async Task<IActionResult> GetLykkeWalletTokenMobile()
        {
            var userId = _userSessionManager.GetIdFromCookie();

            return await GetTokenBasedOnCookie(userId);
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

        private async Task<IActionResult> GetTokenBasedOnCookie(string userId)
        {
            if (userId == null) return Unauthorized();

            var session = await _userSessionRepository.GetAsync(userId);
            var oldLykkeToken = session.Get<string>("OldLykkeToken");
            if (oldLykkeToken == null) return Unauthorized();

            var lykkeClientId = session.Get<string>("LykkeClientId");

            var clientAccount = await _clientAccountClient.GetByIdAsync(lykkeClientId);

            var authId = session.Get<string>("AuthId");

            return new JsonResult(new
            {
                Token = oldLykkeToken,
                AuthId = authId,
                clientAccount.NotificationsId
            });
        }

        [HttpGet("~/token/kyc")]
        public async Task<IActionResult> GetKycToken()
        {
            var userSession = await _userSessionManager.GetUserSession();

            if (userSession == null)
            {
                _logger.LogWarning("No user session.");
                return Unauthorized();
            }

            var tokens = userSession.Get<TokenData>("IroncladTokenResponse");

            var oldLykkeToken = userSession.Get<string>("OldLykkeToken");

            if (tokens == null)
            {
                _logger.LogWarning("No tokens in user session.");
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(oldLykkeToken))
            {
                _logger.LogWarning("No old lykke token in user session.");
                return Unauthorized();
            }

            return new JsonResult(new {Token = tokens.AccessToken});
        }
    }
}
