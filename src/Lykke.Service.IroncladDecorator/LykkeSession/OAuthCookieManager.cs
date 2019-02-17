using System.Linq;
using System.Security.Cryptography;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Lykke.Service.IroncladDecorator.LykkeSession
{
    public class OAuthCookieManager : IOAuthCookieManager
    {
        private const string OAuthCookieName = "ServerCookie";

        private const string SessionIdClaimType = "http://lykke.com/oauth/sessionid";
        
        private const string DefaultPurpose =
            "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware";

        private static readonly string FullOAuthCookieName =
            $"{CookieAuthenticationDefaults.CookiePrefix}{OAuthCookieName}";

        private readonly HttpContextAccessor _httpContextAccessor;
        
        private readonly ISecureDataFormat<AuthenticationTicket> _secureDataFormat;

        public OAuthCookieManager(
            HttpContextAccessor httpContextAccessor,
            IDataProtectionProvider dataProtectionProvider)
        {
            _httpContextAccessor = httpContextAccessor;

            var dataProtector = dataProtectionProvider.CreateProtector(DefaultPurpose, OAuthCookieName, "v2");

            _secureDataFormat = new TicketDataFormat(dataProtector);
        }

        public OAuthCookieData GetOAuthCookieData()
        {
            try
            {
                var cookieExist =
                    _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(FullOAuthCookieName,
                        out var cookieValue);

                if (!cookieExist)
                    return null;

                var ticket = _secureDataFormat.Unprotect(cookieValue);

                return GetCookieData(ticket);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        private static OAuthCookieData GetCookieData(AuthenticationTicket ticket)
        {
            var lykkeToken =
                ticket.Principal.Claims.FirstOrDefault(claim => string.Equals(claim.Type, SessionIdClaimType))?.Value;
            
            var lykkeUserId =
                ticket.Principal.Claims.FirstOrDefault(claim => string.Equals(claim.Type, JwtClaimTypes.Subject))?.Value;

            return new OAuthCookieData(lykkeToken, lykkeUserId);
        }
    }
}
