namespace Lykke.Service.IroncladDecorator.Settings
{
    public class IroncladSettings
    {
        public IdentityProviderSettings IroncladIdp { get; set; }
        public IdentityProviderClientSettings AuthClient { get; set; }
        public IdentityProviderClientSettings AndroidClient { get; set; }
        public IdentityProviderClientSettings IosClient { get; set; }
        public IdentityProviderClientSettings IntrospectionClient { get; set; }
    }
}
