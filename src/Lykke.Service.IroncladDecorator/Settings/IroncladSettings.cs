namespace Lykke.Service.IroncladDecorator.Settings
{
    public class IroncladSettings
    {
        public IdentityProviderSettings IroncladIdp { get; set; }
        public IdentityProviderClientSettings AuthClient { get; set; }
        public IdentityProviderClientSettings IntrospectionClient { get; set; }
    }
}
