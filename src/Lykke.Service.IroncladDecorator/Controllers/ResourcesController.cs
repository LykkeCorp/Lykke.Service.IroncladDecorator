using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
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
        private readonly IIroncladFacade _ironcladFacade;
        private readonly LifetimeSettings _lifetimeSettings;
        private readonly ILykkeSessionManager _lykkeSessionManager;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IUserSessionRepository _userSessionRepository;

        public ResourcesController(
            IUserSessionManager userSessionManager,
            IUserSessionRepository userSessionRepository,
            IClientAccountClient clientAccountClient,
            IIroncladFacade ironcladFacade,
            IClientSessionsClient clientSessionsClient,
            LifetimeSettings lifetimeSettings,
            ILykkeSessionManager lykkeSessionManager)
        {
            _lifetimeSettings = lifetimeSettings;
            _lykkeSessionManager = lykkeSessionManager;
            _userSessionRepository = userSessionRepository;
            _clientAccountClient = clientAccountClient;
            _userSessionManager = userSessionManager;
            _ironcladFacade = ironcladFacade;
            _clientSessionsClient = clientSessionsClient;
        }

        [HttpGet("~/getlykkewallettoken")]
        public async Task<IActionResult> GetLykkewalletToken()
        {
            var bearerToken = HttpContext.GetBearerTokenFromAuthorizationHeader();

            if (bearerToken == null)
                return Unauthorized();

            var introspectionResponse = await _ironcladFacade.IntrospectTokenAsync(bearerToken);

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
                tokens = await _ironcladFacade.RefreshIroncladTokensAsync(tokens.RefreshToken);

            lykkeSession.IroncladTokens = tokens;

            var newLykkeSession = new LykkeSession(oldLykkeToken, tokens);

            await _lykkeSessionManager.SetAsync(newLykkeSession);

            return new JsonResult(new {token = tokens.AccessToken});
        }
        
        private async Task<IActionResult> GetTokenBasedOnCookie(string sessionId)
        {
            if (sessionId == null) return Unauthorized();

            var session = await _userSessionRepository.GetAsync(sessionId);
            if (session == null) return Unauthorized();

            var oldLykkeToken = session.OldLykkeToken;
            if (oldLykkeToken == null) return Unauthorized();

            var lykkeClientId = session.LykkeUserId;

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
    }
}
