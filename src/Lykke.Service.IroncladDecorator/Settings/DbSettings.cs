using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class DbSettings
    {
        [AzureTableCheck] public string LogsConnString { get; set; }
        public DataProtectionSettings DataProtectionSettings { get; set; }
        public string RedisConnString { get; set; }
        public string ClientPersonalInfoConnString { get; set; }
    }
}
