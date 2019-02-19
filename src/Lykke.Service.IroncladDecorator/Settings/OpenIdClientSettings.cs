using System.Collections.Generic;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class OpenIdClientSettings : BaseOpenIdClientSettings
    {
        public string CallbackPath { get; set; }
        [Optional] public string ResponseType { get; set; }
        [Optional] public string ResponseMode { get; set; }
        [Optional] public IEnumerable<string> Scopes { get; set; }
        [Optional] public bool? RequireHttpsMetadata { get; set; }
        [Optional] public bool? GetClaimsFromUserInfoEndpoint { get; set; }
    }
}
