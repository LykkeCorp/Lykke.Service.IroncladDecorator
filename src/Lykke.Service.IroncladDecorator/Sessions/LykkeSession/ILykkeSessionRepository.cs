using System.Threading.Tasks;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public interface ILykkeSessionRepository
    {
        Task SetAsync(LykkeSession lykkeSession);

        Task<LykkeSession> GetAsync(string oldLykkeToken);

        Task DeleteAsync(string oldLykkeToken);
    }
}
