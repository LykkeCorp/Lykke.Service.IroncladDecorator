using System;
using System.Linq;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.IroncladDecorator.Extensions
{
    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddMobileClient(
            this AuthenticationBuilder builder,
            string scheme,
            OpenIdClientSettings clientSettings,
            string authority)
        {
            return builder.AddOpenIdConnect(scheme, options =>
            {
                if (clientSettings == null) throw new ArgumentNullException(nameof(clientSettings));

                options.ClaimActions.MapAll();

                options.SignInScheme = Constants.Cookies.DefaultSignInCookie;

                options.DisableTelemetry = true;

                options.SaveTokens = false;

                options.EventsType = typeof(CustomOpenIdConnectEvents);

                // Set unique callback path for every provider to eliminate intersection.
                options.CallbackPath = clientSettings.CallbackPath;

                options.Authority = authority;

                options.ClientId = clientSettings.ClientId;

                if (!string.IsNullOrWhiteSpace(clientSettings.ResponseType))
                    options.ResponseType = clientSettings.ResponseType;

                if (!string.IsNullOrWhiteSpace(clientSettings.ResponseMode))
                    options.ResponseMode = clientSettings.ResponseMode;

                if (!string.IsNullOrWhiteSpace(clientSettings.ClientSecret))
                    options.ClientSecret = clientSettings.ClientSecret;

                if (clientSettings.RequireHttpsMetadata.HasValue)
                    options.RequireHttpsMetadata = clientSettings.RequireHttpsMetadata.Value;

                if (clientSettings.GetClaimsFromUserInfoEndpoint.HasValue)
                    options.GetClaimsFromUserInfoEndpoint = clientSettings.GetClaimsFromUserInfoEndpoint.Value;

                if (clientSettings.Scopes != null)
                {
                    options.Scope.Clear();
                    var validatedScopes = clientSettings.Scopes.Where(s => !string.IsNullOrWhiteSpace(s));
                    foreach (var scope in validatedScopes)
                    {
                        options.Scope.Add(scope);
                    }
                }
            });
        }
    }
}
