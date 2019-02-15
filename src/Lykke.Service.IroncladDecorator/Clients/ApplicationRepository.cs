using System.Threading.Tasks;
using AzureStorage;
using Common;

namespace Lykke.Service.IroncladDecorator.Clients
{
    public class ApplicationRepository : IApplicationRepository
    {
        private readonly INoSQLTableStorage<ApplicationEntity> _applicationTablestorage;

        public ApplicationRepository(INoSQLTableStorage<ApplicationEntity> applicationTablestorage)
        {
            _applicationTablestorage = applicationTablestorage;
        }

        public async Task<ClientApplication> GetByIdAsync(string id)
        {
            if (!id.IsValidPartitionOrRowKey())
            {
                return null;
            }
            var partitionKey = ApplicationEntity.GeneratePartitionKey();
            var rowKey = ApplicationEntity.GenerateRowKey(id);

            var application = await _applicationTablestorage.GetDataAsync(partitionKey, rowKey);
            return ClientApplication.Create(application);
        }
    }
}
