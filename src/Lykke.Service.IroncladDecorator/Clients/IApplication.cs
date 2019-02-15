namespace Lykke.Service.IroncladDecorator.Clients
{
    public interface IApplication
    {
        string ApplicationId { get; }
        string DisplayName { get; }
        string RedirectUri { get; }
        string Secret { get; }
        string Type { get; }
        OAuthClientProperties OAuthClientProperties { get; }
    }
}
