using System;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public class AuthenticationResponseContext
    {
        public string Code { get; }

        private readonly OpenIdConnectMessage _openIdConnectMessage;

        private readonly OpenIdConnectProtocolValidator _connectProtocolValidator = 
            new OpenIdConnectProtocolValidator
        {
            RequireTimeStampInNonce = false
        };

        public AuthenticationResponseContext(OpenIdConnectMessage connectMessage)
        {
            _openIdConnectMessage = connectMessage;
            Code = connectMessage.Code;
        }

        public void Validate(AuthorizationRequestContext authorizationRequestContext)
        {
            if (authorizationRequestContext == null)
                throw new ArgumentNullException(nameof(authorizationRequestContext));

            var context = authorizationRequestContext.GetValidationContext();

            context.ProtocolMessage = _openIdConnectMessage;

            _connectProtocolValidator.ValidateAuthenticationResponse(context);
        }
    }
}
