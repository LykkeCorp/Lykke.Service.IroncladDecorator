using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class DbSettings
    {
        [AzureTableCheck] public string LogsConnString { get; set; }
    }
}
