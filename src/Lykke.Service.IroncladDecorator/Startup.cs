using System;
using System.Net.Http;
using IdentityModel;
using IdentityModel.Client;
using JetBrains.Annotations;
using Lykke.Sdk;
using Lykke.Service.IroncladDecorator.Extensions;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
using Lykke.Service.IroncladDecorator.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddTransient<CustomOpenIdConnectEvents>();

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
            
                    var ironcladIdp = currentSettings.IroncladDecoratorService.IroncladSettings.IroncladIdp;

                    sc.AddAuthentication(
                            authOptions =>
                            {
                                authOptions.DefaultScheme = "Cookies";
                                authOptions.DefaultChallengeScheme = "oidc";
                            })
                        .AddCookie("Cookies")
                        .AddMobileClient("android",
                            currentSettings.IroncladDecoratorService.IroncladSettings.AndroidClient)
                        .AddMobileClient("ios", currentSettings.IroncladDecoratorService.IroncladSettings.IosClient);

                    sc.AddHttpContextAccessor();

                    services.AddLykkeAzureBlobDataProtection(
                        currentSettings.IroncladDecoratorService.Db.DataProtectionSettings);

                    services.AddDistributedRedisCache(cacheOptions =>
                    {
                        cacheOptions.Configuration = currentSettings.IroncladDecoratorService.Db.RedisConnString;
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

            app.UseAuthentication();

            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
            });
        }

    }
}
