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
            IdentityToken = tokenResponse.IdentityToken;
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;
            TokenType = tokenResponse.TokenType;
            ExpiresIn = tokenResponse.ExpiresIn;
            //TODO Refactor to factory.
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        }

        public TokenData(OpenIdConnectMessage tokenResponse)
        {
            var expiresIn = Convert.ToInt32(tokenResponse.ExpiresIn);
            IdentityToken = tokenResponse.IdToken;
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;
            TokenType = tokenResponse.TokenType;
            ExpiresIn = expiresIn;
            //TODO Refactor to factory.
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        }

        public string IdentityToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
