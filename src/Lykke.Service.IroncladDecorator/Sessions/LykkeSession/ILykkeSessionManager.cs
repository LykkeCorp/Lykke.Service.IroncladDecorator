using System.Threading.Tasks;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public interface ILykkeSessionManager
    {
        Task<LykkeSession> GetActiveAsync(string oldLykkeToken);

        Task SetAsync(LykkeSession session);

        Task CreateAsync(string oldLykkeToken, TokenData tokens);

        Task DeleteAsync(string oldLykkeToken);
    }
}
