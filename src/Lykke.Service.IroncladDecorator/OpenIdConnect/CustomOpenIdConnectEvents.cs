using System.Threading.Tasks;
using Lykke.Service.IroncladDecorator.UserSession;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Lykke.Service.IroncladDecorator.OpenIdConnect
{
    public class CustomOpenIdConnectEvents : OpenIdConnectEvents
    {
        public override Task RedirectToIdentityProvider(RedirectContext context)
        {
            var protocolMessage = context.ProtocolMessage;

            var properties = context.Properties;
            
            if (!string.IsNullOrEmpty(protocolMessage.State))
                properties.Items[OpenIdConnectDefaults.UserstatePropertiesKey] = protocolMessage.State;
            properties.Items[OpenIdConnectDefaults.RedirectUriForCodePropertiesKey] = protocolMessage.RedirectUri;
            protocolMessage.State = context.Options.StateDataFormat.Protect(properties);

            //note: save correct auth url to redirect to it in ConnectController.
            context.HttpContext.Items.Add("OpenIdConnectAuthenticationRequestUrl", protocolMessage.CreateAuthenticationRequestUrl());

            context.HandleResponse();

            return Task.CompletedTask;
        }

        public override Task TokenValidated(TokenValidatedContext context)
        {
            context.SkipHandler();

            var tokenData = new TokenData(context.TokenEndpointResponse);

            //note: save tokens to get them in CallbackController.
            context.HttpContext.Items["CallbackTokenEndpointResponse"] = tokenData;

            return Task.CompletedTask;
        }
    }
}
