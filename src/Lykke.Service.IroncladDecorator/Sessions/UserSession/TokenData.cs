using System;
using IdentityModel.Client;
using MessagePack;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    [MessagePackObject(true)]
    public class TokenData
    {
        public IdentityToken IdentityToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }

        public TokenData()
        {
        }

        public TokenData(TokenResponse tokenResponse)
        {
            IdentityToken = new IdentityToken
            {
                Source = tokenResponse.IdentityToken
            };
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;
            TokenType = tokenResponse.TokenType;
            ExpiresIn = tokenResponse.ExpiresIn;
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        }

        public TokenData(OpenIdConnectMessage tokenResponse)
        {
            var expiresIn = Convert.ToInt32(tokenResponse.ExpiresIn);
            IdentityToken = new IdentityToken
            {
                Source = tokenResponse.IdToken
            };
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;
            TokenType = tokenResponse.TokenType;
            ExpiresIn = expiresIn;
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
        }
    }
}
