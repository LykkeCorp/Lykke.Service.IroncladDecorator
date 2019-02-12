using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.UserSession;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthenticationController : Controller
    {
        private readonly IUserSessionManager _userSessionManager;
        private readonly IDiscoveryCache _discoveryCache;

        public AuthenticationController(
            IUserSessionManager userSessionManager,
            IDiscoveryCache discoveryCache)
        {
            _userSessionManager = userSessionManager;
            _discoveryCache = discoveryCache;
        }

        [HttpGet("~/signin/{platform?}")]
        public async Task<IActionResult> Login([FromRoute] string platform, [FromQuery] string returnUrl = null)
        {
            var userSession = new UserSession.UserSession();

            userSession.Set("SigninPlatform", platform);
            userSession.Set("SigninReturnUrl", returnUrl);

            await _userSessionManager.SetUserSession(userSession);

            return await RedirectToExternalProvider();
        }

        private async Task<ActionResult> RedirectToExternalProvider()
        {
            var signinCallback = Url.AbsoluteAction("SigninCallback", "Callback");

            var discoveryResponse = await _discoveryCache.GetAsync();

            if (discoveryResponse.IsError)
            {
                _discoveryCache.Refresh();
                throw new Exception(discoveryResponse.Error);
            }

            var authorizeRequest = new Dictionary<string, string>
            {
                {OidcConstants.AuthorizeRequest.ClientId, "2828d97c-a866-492f-badc-ad2350b5de2f"},
                {OidcConstants.AuthorizeRequest.RedirectUri, signinCallback},
                {OidcConstants.AuthorizeRequest.Scope, "profile openid email lykke offline_access"},
                {OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code},
                {OidcConstants.AuthorizeRequest.Nonce, "mn4vcynp2tOEj7W9m88l"},
                {OidcConstants.AuthorizeRequest.State, "ttoY604BgSsliwgcnIt8"}
            };

            var query = QueryString.Create(authorizeRequest);

            var externalAuthorizeUrl = $"{discoveryResponse.AuthorizeEndpoint}{query.ToUriComponent()}";

            return Redirect(externalAuthorizeUrl);
        }
    }
}
