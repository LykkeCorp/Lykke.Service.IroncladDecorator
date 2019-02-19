using System.Threading.Tasks;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public interface IUserSessionManager
    {
        void DeleteSessionCookie();

        void CreateIdCookie(UserSession userSession);

        string GetIdFromCookie();

        Task<UserSession> GetUserSession();

        Task SetUserSession(UserSession userSession);
    }
}
