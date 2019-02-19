using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class BaseOpenIdClientSettings
    {
        public string ClientId { get; set; }
        [Optional] public string ClientSecret { get; set; }
    }
}
