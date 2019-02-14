using System;
using System.Threading.Tasks;
using Lykke.Service.IroncladDecorator.Settings;
using Lykke.Service.IroncladDecorator.UserSession;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Lykke.Service.IroncladDecorator.LykkeSession
{
    internal class LykkeSessionRepository : ILykkeSessionRepository
    {
        private const string LykkeSessionProtector = nameof(LykkeSessionProtector);

        private const string Prefix = "IroncladDecorator:LykkeSessions";

        private readonly IDataProtector _dataProtector;

        private readonly IDistributedCache _distributedCache;

        private readonly DistributedCacheEntryOptions _cacheEntryOptions;

        public LykkeSessionRepository(
            LifetimeSettings lifetimeSettings,
            IDataProtectionProvider dataProtectionProvider,
            IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
            _dataProtector =
                dataProtectionProvider.CreateProtector(LykkeSessionProtector);

            _cacheEntryOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = lifetimeSettings.LykkeSessionRedisSlidingExpiration
            };
        }

        public Task SetAsync(LykkeSession lykkeSession)
        {
            if (lykkeSession == null)
                throw new ArgumentNullException(nameof(lykkeSession));

            var id = lykkeSession.OldLykkeToken;

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Old lykke token is empty.");

            var session = ProtectionUtils.SerializeAndProtect(lykkeSession, _dataProtector);

            return _distributedCache.SetStringAsync(GetKey(id), session, _cacheEntryOptions);
        }

        public async Task<LykkeSession> GetAsync(string oldLykkeToken)
        {
            if (string.IsNullOrWhiteSpace(oldLykkeToken))
                return null;

            var value = await _distributedCache.GetStringAsync(GetKey(oldLykkeToken));

            if (string.IsNullOrWhiteSpace(value))
                return null;

            var session = ProtectionUtils.DeserializeAndUnprotect<LykkeSession>(value, _dataProtector);
            
            return session;
        }

        public Task DeleteAsync(string oldLykkeToken)
        {
            return _distributedCache.RemoveAsync(GetKey(oldLykkeToken));
        }

        private string GetKey(string id)
        {
            var protectedId = ProtectionUtils.ComputeSha256Hash(id);
            return $"{Prefix}:{protectedId}";
        }
    }
}
