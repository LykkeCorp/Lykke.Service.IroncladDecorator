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
using Lykke.Service.IroncladDecorator.Sessions;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
using Lykke.Service.IroncladDecorator.Settings;
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

            var tokens = await GetTokens(authCode);

            await SignInToLykke(tokens.IdentityToken, userSession, tokens);

            var query = userSession.AuthorizeQuery;

            var redirectUri = BuildFragmentRedirectUri(query, tokens);

            _log.Info("Redirecting to client app redirect uri. RedirectUri:{RedirectUri}", redirectUri);
            return Redirect(redirectUri);
        }

        private async Task SignInToLykke(IdentityToken identityToken, UserSession userSession, TokenData tokens)
        {
            var authResult = await _clientSessionsClient.Authenticate(identityToken.UserId, "hobbit");

            userSession.SaveAuthResult(authResult, tokens);
            await _userSessionManager.SetUserSession(userSession);

            await _lykkeSessionManager.CreateAsync(authResult.SessionToken, tokens);
        }

        [HttpGet]
        [Route("signin-oidc-android")]
        public async Task<IActionResult> SigninCallbackAndroid()
        {
            await ProcessMobileCallback();

            return RedirectToAction("GetLykkeWalletTokenMobile", "Resources");
        }

        [HttpGet]
        [Route("signin-oidc-ios")]
        public async Task<IActionResult> SigninCallbackIos()
        {
            await ProcessMobileCallback();

            return RedirectToAction("AfterLoginIos");
        }

        [HttpGet]
        [Route("signin-oidc-ios-afterlogin")]
        public IActionResult AfterLoginIos()
        {
            return Ok();
        }

        private async Task ProcessMobileCallback()
        {
            if (!HttpContext.Items.TryGetValue(Constants.Callback.TokenEndpointResponse, out var tokensValue))
            {
                throw new Exception("No token result in callback.");
            }

            if(!(tokensValue is TokenData tokens))
                throw new Exception("Could not cast to token data.");
            
            var userSession = await _userSessionManager.GetUserSession() ?? new UserSession();

            await SignInToLykke(tokens.IdentityToken, userSession, tokens);
        }

        private async Task<TokenData> GetTokens(string authCode)
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
                ClientId =  _ironcladSettings.AuthClient.ClientId,
                ClientSecret = _ironcladSettings.AuthClient.ClientSecret,
                RedirectUri = Url.AbsoluteAction("SigninCallback", "Callback")
            });

            if (tokenResponse.IsError)
                throw new Exception(tokenResponse.Error);

            return new TokenData(tokenResponse);
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

            dict[OidcConstants.TokenResponse.IdentityToken] = tokens.IdentityToken.Source;
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
