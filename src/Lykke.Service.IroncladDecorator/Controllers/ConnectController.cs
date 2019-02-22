using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using IdentityModel;
using Lykke.Common.Log;
using Lykke.Service.IroncladDecorator.Clients;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
using Lykke.Service.IroncladDecorator.Sessions;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    [Route("connect")]
    public class ConnectController : ControllerBase
    {
        private readonly ILog _log;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IIroncladFacade _ironcladFacade;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IroncladSettings _ironcladSettings;

        public ConnectController(
            ILogFactory logFactory,
            IUserSessionManager userSessionManager,
            IIroncladFacade ironcladFacade,
            IApplicationRepository applicationRepository, 
            IroncladSettings ironcladSettings
            )
        {
            _applicationRepository = applicationRepository;
            _ironcladSettings = ironcladSettings;
            _log = logFactory.CreateLog(this);
            _userSessionManager = userSessionManager;
            _ironcladFacade = ironcladFacade;
        }

        [HttpGet]
        [Route("authorize")]
        public async Task<ActionResult> Authorize()
        {
            var error = await ValidateQuery();
            if (!string.IsNullOrEmpty(error))
                return BadRequest(error);

            var requestMessage = HttpContext.GetOpenIdConnectMessage();

            var userSession = new UserSession(requestMessage);
            await _userSessionManager.SetUserSession(userSession);

            var query = CreateAuthenticationRequestUrl(requestMessage, Url.AbsoluteAction("SigninCallback", "Callback"));

            return await RedirectToExternalProvider(query);
        }

        [HttpGet]
        [Route("authorize-old")]
        public async Task<ActionResult> AuthorizeOld()
        {
            var error = await ValidateQuery();
            if (!string.IsNullOrEmpty(error))
                return BadRequest(error);

            var requestMessage = HttpContext.GetOpenIdConnectMessage();

            var userSession = new UserSession(requestMessage);
            await _userSessionManager.SetUserSession(userSession);

            var query = CreateAuthenticationRequestUrl(requestMessage, Url.AbsoluteAction("SigninCallbackOld", "Callback"));

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
            var discoveryResponse = await _ironcladFacade.GetDiscoveryResponseAsync();

            var uriBuilder = new UriBuilder(discoveryResponse.AuthorizeEndpoint)
            {
                Query = query
            };

            var externalAuthorizeUrl = uriBuilder.ToString();
            _log.Info(
                $"Redirect URI substitued, trying to proxy to external provider on {externalAuthorizeUrl}");

            return Redirect(externalAuthorizeUrl);
        }

        private string CreateAuthenticationRequestUrl(OpenIdConnectMessage requestMessage, string redirectUri)
        {
            var newMessage = requestMessage.Clone();


            newMessage.ResponseType = OidcConstants.ResponseTypes.Code;

            newMessage.ClientId = _ironcladSettings.AuthClient.ClientId;
            newMessage.ClientSecret = _ironcladSettings.AuthClient.ClientSecret;
            newMessage.RedirectUri = redirectUri;

            newMessage.Scope = $"{OpenIdConnectScope.Email} {OpenIdConnectScope.OpenId}";
            return newMessage.CreateAuthenticationRequestUrl();
        }
    }
}
