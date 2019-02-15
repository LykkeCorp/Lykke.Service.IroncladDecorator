using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.IroncladDecorator.Clients
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthorizationFlow
    {
        AuthorizationCode,
        Implicit
    }
}
