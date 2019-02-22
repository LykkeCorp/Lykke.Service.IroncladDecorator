using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using IdentityModel;
using IdentityModel.Client;
using Lykke.Common.Log;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.Sessions;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    [Route("callback")]
    public class CallbackController : Controller
    {
        private const string SessinNotExistMessage = "User session does not exist.";
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly IIroncladFacade _ironcladFacade;
        private readonly ILog _log;
        private readonly IUserSessionManager _userSessionManager;
        private readonly ILykkeSessionManager _lykkeSessionManager;
        private readonly ITokenValidationParametersFactory _tokenValidationParametersFactory;

        public CallbackController(
            IUserSessionManager userSessionManager,
            ILogFactory logFactory,
            IClientSessionsClient clientSessionsClient,
            IIroncladFacade ironcladFacade,
            ILykkeSessionManager lykkeSessionManager,
            ITokenValidationParametersFactory tokenValidationParametersFactory)
        {
            _clientSessionsClient = clientSessionsClient;
            _userSessionManager = userSessionManager;
            _log = logFactory.CreateLog(this);
            _ironcladFacade = ironcladFacade;
            _lykkeSessionManager = lykkeSessionManager;
            _tokenValidationParametersFactory = tokenValidationParametersFactory;
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

            var authenticationResponseContext = new AuthenticationResponseContext(HttpContext.GetOpenIdConnectMessage());
            
            var authCode = authenticationResponseContext.Code;

            authenticationResponseContext.Validate(userSession.AuthorizationRequestContext);
            
            var tokenResponse = await GetTokenResponse(authCode);

            var tokenData = await ValidateTokenResponse(userSession.AuthorizationRequestContext, tokenResponse);
           
            await SignInToLykkeAsync(tokenData.IdentityToken, userSession, tokenData);

            var redirectUri = BuildFragmentRedirectUri(userSession, tokenData);

            _log.Info("Redirecting to client app redirect uri. RedirectUri:{RedirectUri}", redirectUri);

            return Redirect(redirectUri);
        }

        private async Task SignInToLykkeAsync(IdentityToken identityToken, UserSession userSession, TokenData tokens)
        {
            var authResult = await _clientSessionsClient.Authenticate(identityToken.UserId, "hobbit");

            await SaveLykkeSession(authResult.SessionToken, tokens);

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

            await SignInToLykkeAsync(tokens.IdentityToken, userSession, tokens);
        }

        private Task<TokenResponse> GetTokenResponse(string authCode)
        {
            return _ironcladFacade.RedeemAuthorizationCodeAsync(
                authCode,
                Url.AbsoluteAction("SigninCallback", "Callback")
            );
        }

        private async Task<TokenData> ValidateTokenResponse(
            AuthorizationRequestContext authorizationRequestContext,
            TokenResponse tokenResponse)
        {
            var tokenResponseContext = new TokenResponseContext(tokenResponse);

            var discoveryResponse = await _ironcladFacade.GetDiscoveryResponseAsync();

            var keys = discoveryResponse.GetSecurityKeys();

            var validationParameters = _tokenValidationParametersFactory.CreateTokenValidationParameters(keys);

            tokenResponseContext.Validate(authorizationRequestContext, validationParameters);

            return new TokenData(tokenResponse);
        }

        private Task SaveLykkeSession(string oldLykkeToken, TokenData tokens)
        {
            var lykkeSession = new LykkeSession(oldLykkeToken, tokens);
            return _lykkeSessionManager.SetAsync(lykkeSession);
        }

        private string BuildFragmentRedirectUri(
            UserSession userSession,
            TokenData tokens)
        {
            _log.Info("Start building fragment redirect uri.");
            if (userSession == null)
            {
                _log.Warning("No user session!");
                return null;
            }

            if (tokens == null)
            {
                _log.Warning("Token response is null!");
                return null;
            }

            var originalAuthorizationRequest = userSession.AuthorizationRequestContext;

            var redirectUri = originalAuthorizationRequest.RedirectUri;
            
            var state = originalAuthorizationRequest.State;

            if (string.IsNullOrEmpty(redirectUri) || string.IsNullOrEmpty(state))
                return null;

            var dict = new Dictionary<string, string>
            {
                {OidcConstants.AuthorizeRequest.State, state},
                {OidcConstants.TokenResponse.IdentityToken, tokens.IdentityToken.Source},
                {OidcConstants.TokenResponse.AccessToken, tokens.AccessToken},
                {OidcConstants.AuthorizeResponse.ExpiresIn, tokens.ExpiresIn.ToString()},
                {OidcConstants.TokenResponse.TokenType, tokens.TokenType}
            };

            var queryString = QueryString.Create(dict);

            var uriBuilder = new UriBuilder(redirectUri) {Fragment = queryString.ToFragmentString()};

            var resultUri = uriBuilder.Uri.AbsoluteUri;

            return resultUri;
        }
    }
}
