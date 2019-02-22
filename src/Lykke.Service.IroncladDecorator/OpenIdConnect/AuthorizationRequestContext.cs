using MessagePack;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    [MessagePackObject(true)]
    public class AuthorizationRequestContext
    {
        public string State { get; set; }
        public string Nonce { get; set; }
        public string ClientId { get; set; }
        public string RedirectUri { get; set; }

        public AuthorizationRequestContext()
        {
        }

        public AuthorizationRequestContext(OpenIdConnectMessage connectMessage)
        {
            State = connectMessage.State;
            Nonce = connectMessage.Nonce;
            ClientId = connectMessage.ClientId;
            RedirectUri = connectMessage.RedirectUri;
        }

        public OpenIdConnectProtocolValidationContext GetValidationContext()
        {
            return new OpenIdConnectProtocolValidationContext
            {
                ClientId = ClientId,
                Nonce = Nonce,
                State = State
            };
        }
    }
}
