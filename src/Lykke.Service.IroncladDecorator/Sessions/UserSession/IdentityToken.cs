using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using IdentityModel;
using MessagePack;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    [MessagePackObject(true)]
    public class IdentityToken
    {
        private JwtSecurityToken _idToken;
        public string Source { get; set; }

        public string UserId => GetClaim(JwtClaimTypes.Subject);

        private string GetClaim(string claimName)
        {
            InitIdToken();

            var sub = _idToken.Claims.FirstOrDefault(claim => string.Equals(claim.Type, claimName));

            return sub?.Value;
        }

        private void InitIdToken()
        {
            if (_idToken != null) return;
            if (Source == null) return;

            var jwtHandler = new JwtSecurityTokenHandler();
            var readableToken = jwtHandler.CanReadToken(Source);
            if (!readableToken) throw new Exception();
            _idToken = jwtHandler.ReadJwtToken(Source);
        }
    }
}
