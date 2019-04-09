using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Lykke.Service.IroncladDecorator.Sessions
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

        public void CreateIdCookie(UserSession userSession)
        {
            var cookieOptions = CreateCookieOptions();

            var value = ProtectionUtils.SerializeAndProtect(userSession.Id, _dataProtector);

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

        public async Task<UserSession> GetUserSession(string userSessionId = null)
        {
            _log.Info("Start getting session.");

            var id = !string.IsNullOrWhiteSpace(userSessionId) ? userSessionId : GetIdFromCookie();

            if (string.IsNullOrWhiteSpace(id))
            {
                _log.Warning("Session id not found!");
                return null;
            }

            _log.Info("Session found. Id:{Id}", id);

            var session = await _userSessionRepository.GetAsync(id);

            if (session == null) _log.Warning("Session not found in database! Id:{Id}", id);

            _log.Info("Session found in database. Id:{Id}", id);

            return session;
        }

        public async Task SetUserSession(UserSession userSession)
        {
            CreateIdCookie(userSession);

            await _userSessionRepository.SetAsync(userSession);
        }

        public void SetCorrelationCookie(UserSession userSession)
        {
            var useHttps = !_hostingEnvironment.IsDevelopment();

            HttpContext.Response.Cookies.Append("UserSessionCorrelation", userSession.Id, new CookieOptions
            {
                HttpOnly = false,
                Secure = useHttps,
                Expires = _clock.UtcNow.Add(_lifetimeSettings.UserSessionCookieLifetime),
                MaxAge = _lifetimeSettings.UserSessionCookieLifetime,
                SameSite = SameSiteMode.None,
                IsEssential = true
            });
        }

        public string GetCorellationSessionId()
        {
            HttpContext.Request.Cookies.TryGetValue("UserSessionCorrelation", out string value);
            return value;
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
