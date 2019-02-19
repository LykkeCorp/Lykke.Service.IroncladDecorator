using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac.Core.Lifetime;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public class UserSessionRepository : IUserSessionRepository
    {
        private const string IroncladLoginSessionProtector = nameof(IroncladLoginSessionProtector);
        private static readonly string Prefix = "IroncladDecorator:UserSessions";

        private readonly IDataProtector _dataProtector;
        private readonly IDistributedCache _distributedCache;
        private readonly DistributedCacheEntryOptions _cacheEntryOptions;

        public UserSessionRepository(
            LifetimeSettings lifetimeSettings,
            IDataProtectionProvider dataProtectionProvider,
            IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
            _dataProtector =
                dataProtectionProvider.CreateProtector(IroncladLoginSessionProtector);

            _cacheEntryOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = lifetimeSettings.UserSessionRedisSlidingExpiration
            };
        }

        public Task SetAsync(UserSession userUserSession)
        {
            if (userUserSession == null)
                throw new ArgumentNullException(nameof(userUserSession));

            var id = userUserSession.Id;

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Session id is empty.");

            var session = ProtectionUtils.SerializeAndProtect(userUserSession.Data, _dataProtector);

            return _distributedCache.SetStringAsync(GetSessionKey(id), session, _cacheEntryOptions);
        }

        public async Task<UserSession> GetAsync(string id)
        {
            var value = await _distributedCache.GetStringAsync(GetSessionKey(id));
            
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var data = ProtectionUtils.DeserializeAndUnprotect<IDictionary<string, string>>(value, _dataProtector);

            return new UserSession(id, data);
        }

        public Task DeleteAsync(string id)
        {
            return _distributedCache.RemoveAsync(GetSessionKey(id));
        }

        private string GetSessionKey(string id)
        {
            return $"{Prefix}:{id}";
        }
    }
}
