﻿using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.Service.IroncladDecorator.Clients
{
    public class ApplicationEntity : AzureTableEntity, IApplication
    {
        public string ApplicationId => RowKey;
        public string DisplayName { get; set; }
        public string RedirectUri { get; set; }
        public string Secret { get; set; }
        public string Type { get; set; }

        [ValueSerializer(typeof(JsonStorageValueSerializer))]
        public OAuthClientProperties OAuthClientProperties { get; set; }


        public static string GeneratePartitionKey()
        {
            return "InternalApplication";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }
    }
}
