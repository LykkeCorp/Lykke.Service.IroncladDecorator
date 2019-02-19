using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using IdentityModel;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public class IdentityToken
    {
        private readonly JwtSecurityToken _idToken;
        public string Source { get; }

        public IdentityToken(string idTokenSource)
        {
            Source = idTokenSource;

            var jwtHandler = new JwtSecurityTokenHandler();
            var readableToken = jwtHandler.CanReadToken(idTokenSource);
            if (!readableToken) throw new Exception();
            _idToken = jwtHandler.ReadJwtToken(idTokenSource);
        }

        public string UserId => GetClaim(JwtClaimTypes.Subject);

        private string GetClaim(string claimName)
        {
            var sub = _idToken.Claims.FirstOrDefault(claim => string.Equals(claim.Type, claimName));

            return sub?.Value;
        }
    }
}
