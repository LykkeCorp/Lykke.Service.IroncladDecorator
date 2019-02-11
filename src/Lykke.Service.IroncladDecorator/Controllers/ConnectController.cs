using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Common.Log;
using IdentityModel.Client;
using Lykke.Common.Log;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.UserSession;
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

        public ConnectController(
            ILogFactory logFactory,
            IUserSessionManager userSessionManager,
            IDiscoveryCache discoveryCache
        )
        {
            _log = logFactory.CreateLog(this);
            _userSessionManager = userSessionManager;
            _discoveryCache = discoveryCache;
        }

        [HttpGet]
        [Route("authorize")]
        public async Task<ActionResult> Authorize()
        {
            var query = GetQueryString();

            query = await AdaptQueryStringAsync(query);

            return await RedirectToExternalProvider(query);
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

        private async Task<string> AdaptQueryStringAsync(string query)
        {
            await SaveAuthorizeQueryString(query);
            var clientRedirectUri = Request.Query["redirect_uri"];

            var clientRedirectUriEncoded = HttpUtility.UrlEncode(clientRedirectUri);
            var signinCallback = Url.AbsoluteAction("SigninCallback", "Callback");
            var signinCallbackEncoded = HttpUtility.UrlEncode(signinCallback);
            query = Regex.Replace(query, clientRedirectUriEncoded,
                signinCallbackEncoded ?? throw new InvalidOperationException(), RegexOptions.IgnoreCase);

            var responseType = Request.Query["response_type"];
            query = Regex.Replace(query, responseType, "code", RegexOptions.IgnoreCase);

            return query;
        }

        private async Task SaveAuthorizeQueryString(string query)
        {
            var userSession = new UserSession.UserSession();
            userSession.Set("AuthorizeQueryString", query);
            await _userSessionManager.SetUserSession(userSession);
        }

        private string GetQueryString()
        {
            var query = Request.QueryString.Value;
            _log.Info($"Authorize request received, query string {query}");
            return query;
        }
    }
}
