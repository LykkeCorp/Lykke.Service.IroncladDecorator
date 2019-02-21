using System.Threading.Tasks;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public interface IUserSessionRepository
    {
        Task SetAsync(UserSession userSession);

        Task<UserSession> GetAsync(string id);

        Task DeleteAsync(string id);
    }
}
