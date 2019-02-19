using System;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public class TokenData
    {
        public TokenData()
        {
        }

        public TokenData(TokenResponse tokenResponse)
        {
            IdentityToken = new IdentityToken(tokenResponse.IdentityToken);
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;
            TokenType = tokenResponse.TokenType;
            ExpiresIn = tokenResponse.ExpiresIn;
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        }

        public TokenData(OpenIdConnectMessage tokenResponse)
        {
            var expiresIn = Convert.ToInt32(tokenResponse.ExpiresIn);
            IdentityToken = new IdentityToken(tokenResponse.IdToken);
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;
            TokenType = tokenResponse.TokenType;
            ExpiresIn = expiresIn;
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        }

        public IdentityToken IdentityToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
