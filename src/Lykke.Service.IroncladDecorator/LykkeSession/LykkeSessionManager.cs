using System.Threading.Tasks;
using Lykke.Service.Session.Client;

namespace Lykke.Service.IroncladDecorator.LykkeSession
{
    internal class LykkeSessionManager : ILykkeSessionManager
    {
        private readonly ILykkeSessionRepository _lykkeSessionRepository;
        private readonly IClientSessionsClient _clientSessionsClient;
        
        public LykkeSessionManager(
            ILykkeSessionRepository lykkeSessionRepository,
            IClientSessionsClient clientSessionsClient)
        {
            _lykkeSessionRepository = lykkeSessionRepository;
            _clientSessionsClient = clientSessionsClient;
        }

        public async Task<LykkeSession> GetActiveAsync(string oldLykkeToken)
        {
            var isActive = await IsActiveAsync(oldLykkeToken);
            if (!isActive)
            {
                await _lykkeSessionRepository.DeleteAsync(oldLykkeToken);
                return null;
            }

            var lykkeSession = await _lykkeSessionRepository.GetAsync(oldLykkeToken);

            return lykkeSession;
        }

        public Task SetAsync(LykkeSession session)
        {
            return _lykkeSessionRepository.SetAsync(session);
        }

        private async Task<bool> IsActiveAsync(string oldLykkeToken)
        {
            if (string.IsNullOrWhiteSpace(oldLykkeToken))
                return false;

            var clientSession = await _clientSessionsClient.GetAsync(oldLykkeToken);

            return clientSession != null;
        }
    }
}
