using IdentityModel.Client;

namespace Lykke.Service.IroncladDecorator.UserSession
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
        }

        public string IdentityToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}
