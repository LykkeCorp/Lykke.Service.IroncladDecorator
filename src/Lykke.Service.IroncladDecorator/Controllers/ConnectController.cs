using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Common.Log;
using IdentityModel;
using IdentityModel.Client;
using Lykke.Common.Log;
using Lykke.Service.IroncladDecorator.Clients;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    [Route("connect")]
    public class ConnectController : ControllerBase
    {
        private readonly ILog _log;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IDiscoveryCache _discoveryCache;
        private readonly IApplicationRepository _applicationRepository;

        public ConnectController(
            ILogFactory logFactory,
            IUserSessionManager userSessionManager,
            IDiscoveryCache discoveryCache,
            IApplicationRepository applicationRepository
        )
        {
            _applicationRepository = applicationRepository;
            _log = logFactory.CreateLog(this);
            _userSessionManager = userSessionManager;
            _discoveryCache = discoveryCache;
        }

        [HttpGet]
        [Route("authorize")]
        public async Task<ActionResult> Authorize()
        {
            var error = await ValidateQuery();
            if (!string.IsNullOrEmpty(error))
                return BadRequest(error);

            var query = await AdaptQueryStringAsync(Request.QueryString.Value);

            var userSession = new UserSession();

            var authorizationRequest = HttpContext.GetOpenIdConnectMessage();

            userSession.Set("AuthorizationRequest", authorizationRequest);

            var query = GetQueryString();

            query = AdaptQueryString(query);

            await _userSessionManager.SetUserSession(userSession);

            return await RedirectToExternalProvider(query);
        }

        private async Task<string> ValidateQuery()
        {
            var clientId = Request.Query[OidcConstants.AuthorizeRequest.ClientId];
            if (string.IsNullOrEmpty(clientId))
                return OidcConstants.AuthorizeRequest.ClientId + " is required.";

            var clientRedirectUri = Request.Query[OidcConstants.AuthorizeRequest.RedirectUri];
            if (string.IsNullOrEmpty(clientRedirectUri))
                return OidcConstants.AuthorizeRequest.RedirectUri + " is required.";

            var client = await _applicationRepository.GetByIdAsync(clientId);

            if (client == null)
            {
                return OidcConstants.AuthorizeRequest.ClientId + " not found.";
            }
            if (client.RedirectUri.Split(';').FirstOrDefault(x => x == clientRedirectUri) == null)
            {
                return OidcConstants.AuthorizeRequest.RedirectUri + " is invalid";
            }
            return await Task.FromResult(string.Empty);
        }
        
        private async Task<ActionResult> RedirectToExternalProvider(string query)
        {
            var discoveryResponse = await _discoveryCache.GetAsync();

            if (discoveryResponse.IsError)
            {
                _discoveryCache.Refresh();
                throw new Exception(discoveryResponse.Error);
            }

            var externalAuthorizeUrl = $"{discoveryResponse.AuthorizeEndpoint}{query}";
            _log.Info(
                $"Redirect URI substitued, trying to proxy to external provider on {externalAuthorizeUrl}");

            return Redirect(externalAuthorizeUrl);
        }

        private string AdaptQueryString(string query)
        {
            var clientRedirectUri = Request.Query[OidcConstants.AuthorizeRequest.RedirectUri];

            var clientRedirectUriEncoded = HttpUtility.UrlEncode(clientRedirectUri);
            var signinCallback = Url.AbsoluteAction("SigninCallback", "Callback");
            var signinCallbackEncoded = HttpUtility.UrlEncode(signinCallback);
            query = Regex.Replace(query, clientRedirectUriEncoded,
                signinCallbackEncoded ?? throw new InvalidOperationException(), RegexOptions.IgnoreCase);

            var responseType = Request.Query[OidcConstants.AuthorizeRequest.ResponseType];
            query = Regex.Replace(query, responseType, "code", RegexOptions.IgnoreCase);

            return query;
        }

        private async Task<string> AdaptQueryStringAsync(string query)
        {
            await SaveAuthorizeQueryString(query);
            var clientRedirectUri = Request.Query[OidcConstants.AuthorizeRequest.RedirectUri];

            var clientRedirectUriEncoded = HttpUtility.UrlEncode(clientRedirectUri);
            var signinCallback = Url.AbsoluteAction("SigninCallback", "Callback");
            var signinCallbackEncoded = HttpUtility.UrlEncode(signinCallback);
            query = Regex.Replace(query, clientRedirectUriEncoded,
                signinCallbackEncoded ?? throw new InvalidOperationException(), RegexOptions.IgnoreCase);

            var responseType = Request.Query[OidcConstants.AuthorizeRequest.ResponseType];
            query = Regex.Replace(query, responseType, "code", RegexOptions.IgnoreCase);

            return query;
        }

        private async Task SaveAuthorizeQueryString(string query)
        {
            var userSession = new UserSession
            {
                AuthorizeQuery = query
            };
            await _userSessionManager.SetUserSession(userSession);
        }
    }
}
