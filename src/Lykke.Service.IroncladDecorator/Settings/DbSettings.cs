using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class DbSettings
    {
        [AzureTableCheck] public string LogsConnString { get; set; }
        [AzureBlobCheck] public string DataProtectionConnString { get; set; }
        public string RedisConnString { get; set; }
    }
}
