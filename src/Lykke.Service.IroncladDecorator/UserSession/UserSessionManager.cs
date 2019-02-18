using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Lykke.Service.IroncladDecorator.UserSession
{
    public class UserSessionManager : IUserSessionManager
    {
        private const string UserSessionCookieProtector = nameof(UserSessionCookieProtector);
        private const string CookieName = ".AspNetCore.ServerCookie";
        private readonly ISystemClock _clock;
        private readonly IDataProtector _dataProtector;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LifetimeSettings _lifetimeSettings;
        private readonly ILog _log;
        private readonly IUserSessionRepository _userSessionRepository;

        public UserSessionManager(
            IHostingEnvironment hostingEnvironment,
            IHttpContextAccessor httpContextAccessor,
            ISystemClock clock,
            IDataProtectionProvider dataProtectionProvider,
            IUserSessionRepository userSessionRepository,
            ILogFactory logFactory,
            LifetimeSettings lifetimeSettings)
        {
            _hostingEnvironment = hostingEnvironment;
            _httpContextAccessor = httpContextAccessor;
            _clock = clock;
            _userSessionRepository = userSessionRepository;
            _log = logFactory.CreateLog(this);
            _dataProtector = dataProtectionProvider.CreateProtector(UserSessionCookieProtector);
            _lifetimeSettings = lifetimeSettings;
        }

        private HttpContext HttpContext => _httpContextAccessor.HttpContext;

        private bool IsCookieExist => HttpContext.Request.Cookies.ContainsKey(CookieName);

        public void CreateIdCookie(UserSession session)
        {
            var cookieOptions = CreateCookieOptions();

            var value = ProtectionUtils.SerializeAndProtect(session.Id, _dataProtector);

            HttpContext.Response.Cookies.Append(CookieName, value, cookieOptions);
        }

        public void DeleteSessionCookie()
        {
            HttpContext.Response.Cookies.Delete(CookieName);
        }

        public string GetIdFromCookie()
        {
            if (!IsCookieExist)
                return null;

            if (!HttpContext.Request.Cookies.TryGetValue(CookieName, out var value))
                return null;

            return string.IsNullOrWhiteSpace(value)
                ? null
                : ProtectionUtils.DeserializeAndUnprotect<string>(value, _dataProtector);
        }

        public async Task<UserSession> GetUserSession()
        {
            _log.Info("Start getting session.");

            var id = GetIdFromCookie();

            if (string.IsNullOrWhiteSpace(id))
            {
                _log.Warning("Session id not found in cookie!");
                return null;
            }

            _log.Info("Session found in cookie. Id:{Id}", id);

            var session = await _userSessionRepository.GetAsync(id);

            if (session == null) _log.Warning("Session not found in database! Id:{Id}", id);

            _log.Info("Session found in database. Id:{Id}", id);

            return session;
        }

        public async Task SetUserSession(UserSession session, bool updateCookie)
        {
            _log.Info("Start setting session.");

            var idFromCookie = GetIdFromCookie();

            var shouldCreateCookie = updateCookie;

            if (string.IsNullOrEmpty(idFromCookie))
            {
                _log.Info("No session id found in cookie.");
                shouldCreateCookie = true;
            }
            else if (!string.Equals(idFromCookie, session.Id))
            {
                _log.Info(
                    "Session id from cookie is different from new user session to set. CookieSessionId:{CookieSessionId}, NewSessionId:{NewSessionId}",
                    idFromCookie,
                    session.Id);
                shouldCreateCookie = true;
            }

            if (shouldCreateCookie)
            {
                _log.Info("Creating new cookie. Id:{Id}", session.Id);
                CreateIdCookie(session);
            }
            else
            {
                _log.Info("Session cookie already exist.", idFromCookie);
            }

            await _userSessionRepository.SetAsync(session);
        }

        private CookieOptions CreateCookieOptions()
        {
            var useHttps = !_hostingEnvironment.IsDevelopment();

            return new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                Expires = _clock.UtcNow.Add(_lifetimeSettings.UserSessionCookieLifetime),
                MaxAge = _lifetimeSettings.UserSessionCookieLifetime,
                Secure = useHttps,
                //NOTE:@gafanasiev not safe, but this fixes the problem for ios.
                SameSite = SameSiteMode.None
            };
        }
    }
}
