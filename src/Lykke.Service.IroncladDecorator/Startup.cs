using System;
using System.Net.Http;
using IdentityModel.Client;
using JetBrains.Annotations;
using Lykke.Sdk;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace Lykke.Service.IroncladDecorator
{
    [UsedImplicitly]
    public class Startup
    {
        private readonly LykkeSwaggerOptions _swaggerOptions = new LykkeSwaggerOptions
        {
            ApiTitle = "IroncladDecorator API",
            ApiVersion = "v1"
        };

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            return services.BuildServiceProvider<AppSettings>(options =>
            {
                options.SwaggerOptions = _swaggerOptions;

                options.Logs = logs =>
                {
                    logs.AzureTableName = "IroncladDecoratorLog";
                    logs.AzureTableConnectionStringResolver =
                        settings => settings.IroncladDecoratorService.Db.LogsConnString;
                };

                options.Extend = (sc, settings) =>
                {
                    var currentSettings = settings.CurrentValue;

                    sc.AddHttpContextAccessor();

                    services.AddLykkeAzureBlobDataProtection(
                        currentSettings.IroncladDecoratorService.Db.DataProtectionConnString);

                    services.AddDistributedRedisCache(cacheOptions =>
                    {
                        cacheOptions.Configuration = currentSettings.IroncladDecoratorService.Db.RedisConnString;
                    });

                    var ironcladIdp = currentSettings.IroncladDecoratorService.IroncladSettings.IroncladIdp;

                    sc.AddAuthentication(
                            authOptions =>
                            {
                                authOptions.DefaultScheme = "Cookies";
                                authOptions.DefaultChallengeScheme = "oidc";
                            })
                        .AddCookie("Cookies")
                        .AddOpenIdConnect("oidc", idConnectOptions =>
                        {
                            idConnectOptions.SignInScheme = "Cookies";

                            idConnectOptions.Authority = ironcladIdp.Authority;
                            idConnectOptions.RequireHttpsMetadata = false;

                            idConnectOptions.ClientId = ironcladIdp.ClientId;
                            idConnectOptions.ClientSecret = ironcladIdp.ClientSecret;
                            idConnectOptions.ResponseType = "code id_token";

                            idConnectOptions.SaveTokens = true;
                            idConnectOptions.GetClaimsFromUserInfoEndpoint = true;

                            idConnectOptions.Scope.Clear();

                            idConnectOptions.Scope.Add("openid");
                            idConnectOptions.Scope.Add("sample_api");
                            idConnectOptions.Scope.Add("offline_access");
                        });

                    sc.AddHttpClient();

                    sc.AddSingleton<IDiscoveryCache>(r =>
                    {
                        var factory = r.GetRequiredService<IHttpClientFactory>();
                        return new DiscoveryCache(ironcladIdp.Authority, () => factory.CreateClient(),
                            new DiscoveryPolicy
                            {
                                ValidateIssuerName = false
                            });
                    });
                };
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(options =>
            {
                //todo: set restrictions
                options.AllowAnyOrigin();
                options.AllowAnyHeader();
                options.AllowCredentials();
                options.AllowAnyMethod();
            });

            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
            });
        }

    }
}
