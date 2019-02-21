using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public class IdTokenValidationResult
    {
        public ClaimsPrincipal ValidatedClaimsPrincipal { get; set; }
        public JwtSecurityToken ValidatedIdToken { get; set; }
    }
}
