using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Log;
using IdentityModel;
using IdentityModel.Client;
using Lykke.Common.Log;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.LykkeSession;
using Lykke.Service.IroncladDecorator.Settings;
using Lykke.Service.IroncladDecorator.UserSession;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    [Route("callback")]
    public class CallbackController : Controller
    {
        private const string SessinNotExistMessage = "User session does not exist.";
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly IDiscoveryCache _discoveryCache;
        private readonly IroncladSettings _ironcladSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILog _log;
        private readonly IUserSessionManager _userSessionManager;
        private readonly ILykkeSessionManager _lykkeSessionManager;

        public CallbackController(
            IUserSessionManager userSessionManager,
            ILogFactory logFactory,
            IClientSessionsClient clientSessionsClient,
            IHttpClientFactory httpClientFactory,
            IDiscoveryCache discoveryCache,
            IroncladSettings ironcladSettings,
            ILykkeSessionManager lykkeSessionManager)
        {
            _clientSessionsClient = clientSessionsClient;
            _userSessionManager = userSessionManager;
            _log = logFactory.CreateLog(this);
            _httpClientFactory = httpClientFactory;
            _discoveryCache = discoveryCache;
            _ironcladSettings = ironcladSettings;
            _lykkeSessionManager = lykkeSessionManager;
        }

        [HttpGet]
        [Route("signin-oidc")]
        public async Task<IActionResult> SigninCallback()
        {
            _log.Info("Start getting user session.");
            var userSession = await _userSessionManager.GetUserSession();

            if (userSession == null)
            {
                _log.Warning(SessinNotExistMessage);
                return BadRequest(SessinNotExistMessage);
            }
           
            var authCode = HttpContext.Request.Query["code"];

            var tokens = await GetTokens(authCode, _ironcladSettings.AuthClient, Url.AbsoluteAction("SigninCallback", "Callback"));

            var userId = GetUserId(tokens.IdentityToken);

            var authResult = await _clientSessionsClient.Authenticate(userId, "hobbit");

            SaveAuthResult(userSession, authResult);

            SaveTokensToUserSession(userSession, tokens);

            await SaveLykkeSession(authResult.SessionToken, tokens);

            await _userSessionManager.SetUserSession(userSession);

            var query = GetAuthorizeQueryAsync(userSession);

            var redirectUri = BuildFragmentRedirectUri(query, tokens);

            _log.Info("Redirecting to client app redirect uri. RedirectUri:{RedirectUri}", redirectUri);
            return Redirect(redirectUri);
        }

        [HttpGet]
        [Route("signin-oidc-android")]
        public async Task<IActionResult> SigninCallbackAndroid()
        {
            await ProcessMobileCallback(_ironcladSettings.AndroidClient, Url.AbsoluteAction("SigninCallbackAndroid", "Callback"));
            
            return RedirectToAction("GetLykkeWalletTokenMobile", "Resources");
        }

        [HttpGet]
        [Route("signin-oidc-ios")]
        public async Task<IActionResult> SigninCallbackIos()
        {
            await ProcessMobileCallback(_ironcladSettings.IosClient, Url.AbsoluteAction("SigninCallbackIos", "Callback"));

            return RedirectToAction("AfterLoginIos");
        }

        [HttpGet]
        [Route("signin-oidc-ios-afterlogin")]
        public IActionResult AfterLoginIos()
        {
            return Ok();
        }

        private async Task ProcessMobileCallback(IdentityProviderClientSettings clientSettings, string redirectUri)
        {
            var userSession = new UserSession.UserSession();

            var authCode = HttpContext.Request.Query["code"];

            var tokens = await GetTokens(authCode, clientSettings, redirectUri);

            var userId = GetUserId(tokens.IdentityToken);

            var authResult = await _clientSessionsClient.Authenticate(userId, "hobbit");

            SaveAuthResult(userSession, authResult);

            SaveTokensToUserSession(userSession, tokens);

            await SaveLykkeSession(authResult.SessionToken, tokens);

            await _userSessionManager.SetUserSession(userSession);
        }

        private static string GetUserId(string idToken)
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var readableToken = jwtHandler.CanReadToken(idToken);
            if (!readableToken) throw new Exception();
            var token = jwtHandler.ReadJwtToken(idToken);
            var sub = token.Claims.FirstOrDefault(claim => string.Equals(claim.Type, JwtClaimTypes.Subject));

            return sub?.Value;
        }

        private async Task<TokenData> GetTokens(string authCode, IdentityProviderClientSettings clientSettings, string redirectUri)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var discoveryResponse = await _discoveryCache.GetAsync();

            if (discoveryResponse.IsError)
            {
                _discoveryCache.Refresh();
                throw new Exception(discoveryResponse.Error);
            }

            var tokenResponse = await httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                Code = authCode,
                ClientId = clientSettings.ClientId,
                ClientSecret = clientSettings.ClientSecret,
                RedirectUri = redirectUri
            });

            if (tokenResponse.IsError)
                throw new Exception(tokenResponse.Error);

            return new TokenData(tokenResponse);
        }

        private void SaveAuthResult(UserSession.UserSession userSession, IClientSession clientSession)
        {
            userSession.Set("OldLykkeToken", clientSession.SessionToken);
            userSession.Set("AuthId", clientSession.AuthId);
            userSession.Set("LykkeClientId", clientSession.ClientId);
        }

        private Task SaveLykkeSession(string oldLykkeToken, TokenData tokens)
        {
            var lykkeSession = new LykkeSession.LykkeSession(oldLykkeToken, tokens);
            return _lykkeSessionManager.SetAsync(lykkeSession);
        }

        private string GetAuthorizeQueryAsync(UserSession.UserSession session)
        {
            _log.Info("Start getting original authorize query string.");
            return session?.Get<string>("AuthorizeQueryString");
        }

        private void SaveTokensToUserSession(UserSession.UserSession session, TokenData tokens)
        {
            _log.Info("Start saving tokens. TokenResponse:{TokenResponse}", tokens);
            session.Set("IroncladTokenResponse", tokens);
        }

        private string BuildFragmentRedirectUri(
            string query,
            TokenData tokens)
        {
            _log.Info("Start building fragment redirect uri.");
            if (string.IsNullOrWhiteSpace(query))
            {
                _log.Warning("Query string is empty!");
                return null;
            }

            if (tokens == null)
            {
                _log.Warning("Token response is null!");
                return null;
            }

            var parameters = QueryHelpers.ParseQuery(query);

            parameters.TryGetValue(OidcConstants.AuthorizeRequest.RedirectUri, out var redirectUri);

            parameters.TryGetValue(OidcConstants.AuthorizeResponse.State, out var state);

            if (string.IsNullOrEmpty(redirectUri) || string.IsNullOrEmpty(state))
                return null;

            var dict = new Dictionary<string, string>
            {
                {OidcConstants.AuthorizeRequest.State, state}
            };

            dict[OidcConstants.TokenResponse.IdentityToken] = tokens.IdentityToken;
            dict[OidcConstants.TokenResponse.AccessToken] = tokens.AccessToken;
            dict[OidcConstants.AuthorizeResponse.ExpiresIn] = tokens.ExpiresIn.ToString();
            dict[OidcConstants.TokenResponse.TokenType] = tokens.TokenType;

            var queryString = QueryString.Create(dict);

            var uriBuilder = new UriBuilder(redirectUri) {Fragment = queryString.ToFragmentString()};

            var resultUri = uriBuilder.Uri.AbsoluteUri;

            return resultUri;
        }
    }
}
