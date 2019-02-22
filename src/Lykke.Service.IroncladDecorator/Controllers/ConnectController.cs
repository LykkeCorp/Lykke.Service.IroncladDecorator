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

        public ConnectController(
            ILogFactory logFactory,
            IUserSessionManager userSessionManager,
            IIroncladFacade ironcladFacade,
            IApplicationRepository applicationRepository
        )
        {
            _applicationRepository = applicationRepository;
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

            var userSession = new UserSession();

            var requestMessage = HttpContext.GetOpenIdConnectMessage();

            var authorizationRequestContext = new AuthorizationRequestContext(requestMessage);
            
            var query = CreateAuthenticationRequestUrl(requestMessage);

            userSession.AuthorizationRequestContext = authorizationRequestContext;

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

        private string CreateAuthenticationRequestUrl(OpenIdConnectMessage requestMessage)
        {
            var newMessage = requestMessage.Clone();

            newMessage.RedirectUri = Url.AbsoluteAction("SigninCallback", "Callback");

            newMessage.ResponseType = OidcConstants.ResponseTypes.Code;

            return newMessage.CreateAuthenticationRequestUrl();
        }
    }
}
