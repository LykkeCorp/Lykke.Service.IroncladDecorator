using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthenticationController : Controller
    {
        private readonly IDiscoveryCache _discoveryCache;
        private readonly IroncladSettings _ironcladSettings;

        public AuthenticationController(
            IDiscoveryCache discoveryCache, IroncladSettings ironcladSettings)
        {
            _discoveryCache = discoveryCache;
            _ironcladSettings = ironcladSettings;
        }

        [HttpGet("~/signin/{platform?}")]
        public async Task<IActionResult> Login([FromRoute] string platform, [FromQuery] string returnUrl)
        {
            string clientId;
            string signinCallback;

            switch (platform)
            {
                case "android":
                    clientId = _ironcladSettings.AndroidClient.ClientId;
                    signinCallback = Url.AbsoluteAction("SigninCallbackAndroid", "Callback");
                    break;
                case "ios":
                    clientId = _ironcladSettings.IosClient.ClientId;
                    signinCallback = Url.AbsoluteAction("SigninCallbackIos", "Callback");
                    break;
                default:
                    return BadRequest();
            }

            var discoveryResponse = await _discoveryCache.GetAsync();

            if (discoveryResponse.IsError)
            {
                _discoveryCache.Refresh();
                throw new Exception(discoveryResponse.Error);
            }


            var authorizeRequest = new Dictionary<string, string>
            {
                {OidcConstants.AuthorizeRequest.ClientId, clientId},
                {OidcConstants.AuthorizeRequest.RedirectUri, signinCallback},
                {OidcConstants.AuthorizeRequest.Scope, "profile openid email lykke offline_access"},
                {OidcConstants.AuthorizeRequest.ResponseType, OidcConstants.ResponseTypes.Code},
                {OidcConstants.AuthorizeRequest.Nonce, "mn4vcynp2tOEj7W9m88l"},
                {OidcConstants.AuthorizeRequest.State, "ttoY604BgSsliwgcnIt8"}
            };

            var query = QueryString.Create(authorizeRequest);

            var externalAuthorizeUrl = $"{discoveryResponse.AuthorizeEndpoint}{query.ToUriComponent()}";

            switch (platform)
            {
                case "android":
                    return View("~/Views/Redirector.cshtml", model: externalAuthorizeUrl);
                case "ios":
                    return Redirect(externalAuthorizeUrl);
                default:
                    return BadRequest();
            }
        }
    }
}
