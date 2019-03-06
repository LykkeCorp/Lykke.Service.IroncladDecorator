using System.Threading.Tasks;
using IdentityModel.Client;
using Lykke.Service.IroncladDecorator.Sessions;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public interface IIroncladFacade
    {
        Task<DiscoveryResponse> GetDiscoveryResponseAsync();

        Task<TokenResponse> RedeemAuthorizationCodeAsync(string code, string redirectUri);

        Task<TokenData> RefreshIroncladTokensAsync(string ironcladRefreshToken);

        Task<IntrospectionResponse> IntrospectTokenAsync(string bearer);

        Task<TokenRevocationResponse> RevokeTokenAsync(string tokenTypeHint, string token);
    }
}
