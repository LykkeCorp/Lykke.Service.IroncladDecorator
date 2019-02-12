using System.Threading.Tasks;

namespace Lykke.Service.IroncladDecorator.LykkeSession
{
    public interface ILykkeSessionManager
    {
        Task<LykkeSession> GetActiveAsync(string oldLykkeToken);

        Task SetAsync(LykkeSession session);
    }
}
