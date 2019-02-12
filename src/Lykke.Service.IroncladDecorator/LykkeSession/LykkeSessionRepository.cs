using System;
using System.Threading.Tasks;
using Lykke.Service.IroncladDecorator.UserSession;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Lykke.Service.IroncladDecorator.LykkeSession
{
    internal class LykkeSessionRepository : ILykkeSessionRepository
    {
        private const string LykkeSessionProtector = nameof(LykkeSessionProtector);

        private const string Prefix = "OAuth:LykkeSessions";

        private readonly IDataProtector _dataProtector;

        private readonly IDistributedCache _distributedCache;

        public LykkeSessionRepository(
            IDataProtectionProvider dataProtectionProvider,
            IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
            _dataProtector =
                dataProtectionProvider.CreateProtector(LykkeSessionProtector);
        }

        public Task SetAsync(LykkeSession lykkeSession)
        {
            if (lykkeSession == null)
                throw new ArgumentNullException(nameof(lykkeSession));

            var id = lykkeSession.OldLykkeToken;

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Old lykke token is empty.");

            var session = ProtectionUtils.SerializeAndProtect(lykkeSession, _dataProtector);

            return _distributedCache.SetStringAsync(GetKey(id), session);
        }

        public async Task<LykkeSession> GetAsync(string oldLykkeToken)
        {
            if (string.IsNullOrWhiteSpace(oldLykkeToken))
                return null;

            var value = await _distributedCache.GetStringAsync(GetKey(oldLykkeToken));
            var session = ProtectionUtils.DeserializeAndUnprotect<LykkeSession>(value, _dataProtector);
            return session;
        }

        public Task DeleteAsync(string oldLykkeToken)
        {
            return _distributedCache.RemoveAsync(GetKey(oldLykkeToken));
        }

        //TODO protect id to be sure that bare old lykke tokens are not used.
        private string GetKey(string id)
        {
            return $"{Prefix}:{id}";
        }
    }
}
