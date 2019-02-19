namespace Lykke.Service.IroncladDecorator.Settings
{
    public class IroncladSettings
    {
        public IroncladProviderSettings IroncladIdp { get; set; }
        public BaseOpenIdClientSettings AuthClient { get; set; }
        public BaseOpenIdClientSettings IntrospectionClient { get; set; }
        public OpenIdClientSettings AndroidClient { get; set; }
        public OpenIdClientSettings IosClient { get; set; }
    }
}
