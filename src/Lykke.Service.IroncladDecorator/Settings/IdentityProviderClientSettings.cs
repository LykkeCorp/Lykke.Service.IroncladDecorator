using System.Collections.Generic;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class IdentityProviderClientSettings
    {

        public string Id { get; set; }
        public string ClientId { get; set; }
        public string Authority { get; set; }
        [Optional] public string ResponseType { get; set; }
        [Optional] public string ResponseMode { get; set; }
        [Optional] public string ClientSecret { get; set; }
        [Optional] public IEnumerable<string> Scopes { get; set; }
        [Optional] public string CallbackPath { get; set; }
        [Optional] public bool? RequireHttpsMetadata { get; set; }
        [Optional] public bool? GetClaimsFromUserInfoEndpoint { get; set; }
    }
}
