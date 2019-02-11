using System.Threading.Tasks;

namespace Lykke.Service.IroncladDecorator.UserSession
{
    public interface IUserSessionManager
    {
        void DeleteSessionCookie();

        void CreateIdCookie(UserSession session);

        string GetIdFromCookie();

        Task<UserSession> GetUserSession();

        Task SetUserSession(UserSession session);
    }
}
