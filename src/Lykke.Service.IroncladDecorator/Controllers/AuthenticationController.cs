using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.IroncladDecorator.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AuthenticationController : Controller
    {
        [HttpGet("~/signin/{platform?}")]
        public async Task<IActionResult> Login([FromRoute] string platform, [FromQuery] string returnUrl)
        {
            switch (platform)
            {
                case "android":
                case "ios":
                    await HttpContext.ChallengeAsync(platform);
                    break;
                default:
                    return BadRequest("Platform not supported.");
            }

            var isRequestCreated = HttpContext.Items.TryGetValue("OpenIdConnectAuthenticationRequestUrl", out var value);

            if (!isRequestCreated)
                throw new Exception("AuthenticationRequestUrl is not available.");

            var authenticationRequestUrl = value as string;

            if(string.IsNullOrWhiteSpace(authenticationRequestUrl))
                throw new Exception("AuthenticationRequestUrl is empty.");

            switch (platform)
            {
                case "android":
                    return View("~/Views/Redirector.cshtml", authenticationRequestUrl);
                case "ios":
                    return Redirect(authenticationRequestUrl);
                default:
                    return BadRequest();
            }
        }
    }
}
