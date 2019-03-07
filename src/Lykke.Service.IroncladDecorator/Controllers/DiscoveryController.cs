using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    public class DiscoveryController : ControllerBase
    {
        private readonly IIroncladFacade _ironcladFacade;

        public DiscoveryController(IIroncladFacade ironcladFacade)
        {
            _ironcladFacade = ironcladFacade;
        }

        [HttpGet("~/.well-known/openid-configuration")]
        public async Task<ActionResult> OpenIdConfiguration()
        {
            var discoveryResponse = await _ironcladFacade.GetDiscoveryResponseAsync();

            var json = discoveryResponse.Json.DeepClone();

            json["authorization_endpoint"] = Url.AbsoluteAction("Authorize", "Connect");
            json["end_session_endpoint"] = Url.AbsoluteAction("Logout", "Connect");
            json["revocation_endpoint"] = Url.AbsoluteAction("Revocation", "Connect");
            json["jwks_uri"] = Url.AbsoluteAction("Jwks", "Discovery");
            json["userinfo_endpoint"] = Url.AbsoluteAction("UserInfo", "Connect");

            return new JsonResult(json);
        }

        [HttpGet("~/.well-known/openid-configuration/jwks")]
        public async Task<ActionResult> Jwks()
        {
            var response = await _ironcladFacade.GetJwksAsync();

            var result = await response.Content.ReadAsStringAsync();

            var parsed = JsonConvert.DeserializeObject(result);

            return new JsonResult(parsed);
        }
    }
}
