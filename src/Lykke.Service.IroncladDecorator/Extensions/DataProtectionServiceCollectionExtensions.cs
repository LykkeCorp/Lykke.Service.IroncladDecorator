using System;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Lykke.Service.IroncladDecorator.Extensions
{
    public static class DataProtectionServiceCollectionExtensions
    {
        public static IDataProtectionBuilder AddLykkeAzureBlobDataProtection(this IServiceCollection services, DataProtectionSettings dataProtectionSettings)
        {
            if(dataProtectionSettings == null)
                throw new ArgumentNullException(nameof(dataProtectionSettings));

            return services.AddDataProtection()
                // Do not change this value. Otherwise the key will be invalid.
                .SetApplicationName(dataProtectionSettings.AppName)
                .PersistKeysToAzureBlobStorage(
                    SetupDataProtectionStorage(dataProtectionSettings.BlobStorageConnString, dataProtectionSettings.ContainerName),
                    $"{dataProtectionSettings.ContainerName}{dataProtectionSettings.RelativePath}");
        }

        
        private static CloudStorageAccount SetupDataProtectionStorage(string dbDataProtectionConnString, string containerName)
        {
            var storageAccount = CloudStorageAccount.Parse(dbDataProtectionConnString);
            var client = storageAccount.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);

            container.CreateIfNotExistsAsync(new BlobRequestOptions {RetryPolicy = new ExponentialRetry()},
                new OperationContext()).GetAwaiter().GetResult();

            return storageAccount;
        }
    }
}
