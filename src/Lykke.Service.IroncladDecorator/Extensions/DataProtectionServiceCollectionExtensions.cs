using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Lykke.Service.IroncladDecorator.Extensions
{
    public static class DataProtectionServiceCollectionExtensions
    {
        private const string DataProtectionContainerName = "data-protection-container-name";
        private const string DataProtectionAppName = "Lykke.Service.OAuth";

        public static IDataProtectionBuilder AddLykkeAzureBlobDataProtection(this IServiceCollection services, string dataProtectionConnString)
        {
            return services.AddDataProtection()
                // Do not change this value. Otherwise the key will be invalid.
                .SetApplicationName(DataProtectionAppName)
                .PersistKeysToAzureBlobStorage(
                    SetupDataProtectionStorage(dataProtectionConnString),
                    $"{DataProtectionContainerName}/cookie-keys/keys.xml");
        }

        
        private static CloudStorageAccount SetupDataProtectionStorage(string dbDataProtectionConnString)
        {
            var storageAccount = CloudStorageAccount.Parse(dbDataProtectionConnString);
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(DataProtectionContainerName);

            container.CreateIfNotExistsAsync(new BlobRequestOptions {RetryPolicy = new ExponentialRetry()},
                new OperationContext()).GetAwaiter().GetResult();

            return storageAccount;
        }
    }
}
