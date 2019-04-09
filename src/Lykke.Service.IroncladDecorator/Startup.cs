using System;
using System.Net.Http;
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

        private CorsSettings _corsSettings;

        private const string AllowSpecificOrigins = "AllowSpecificOrigins";

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<CustomOpenIdConnectEvents>();

            services.AddCors(options =>
            {
                options.AddPolicy(AllowSpecificOrigins,
                    builder =>
                    {
                        builder.WithOrigins(_corsSettings.CorsOrigins.ToArray());
                        builder.AllowAnyMethod();
                        builder.AllowCredentials();
                        builder.AllowAnyHeader();
                    });
            });

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

                    var ironcladAuthority = currentSettings.IroncladDecoratorService.IroncladSettings.IroncladIdp.Authority;

                    sc.AddAuthentication(
                            authOptions =>
                            {
                                authOptions.DefaultScheme = Constants.Cookies.DefaultSignInCookie;
                                //TODO: think how to handle unauthorized.
                                authOptions.DefaultChallengeScheme = "oidc";
                            })
                        .AddCookie(Constants.Cookies.DefaultSignInCookie)
                        .AddMobileClient(Constants.Platforms.Android,
                            currentSettings.IroncladDecoratorService.IroncladSettings.AndroidClient, ironcladAuthority)
                        .AddMobileClient(Constants.Platforms.Ios,
                            currentSettings.IroncladDecoratorService.IroncladSettings.IosClient, ironcladAuthority);

                    sc.AddHttpContextAccessor();
                    sc.AddLykkeAzureBlobDataProtection(
                        currentSettings.IroncladDecoratorService.Db.DataProtectionSettings);

                    sc.AddDistributedRedisCache(cacheOptions =>
                    {
                        cacheOptions.Configuration = currentSettings.IroncladDecoratorService.Db.RedisConnString;
                    });

                    sc.AddHttpClient();

                    sc.AddSingleton<IDiscoveryCache>(r =>
                    {
                        var factory = r.GetRequiredService<IHttpClientFactory>();
                        return new DiscoveryCache(ironcladAuthority, () => factory.CreateClient(),
                            new DiscoveryPolicy
                            {
                                ValidateIssuerName = false
                            });
                    });

                    sc.AddCors();
                };
            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            _corsSettings = app.ApplicationServices.GetService<CorsSettings>();

            app.UseCors(AllowSpecificOrigins);
                
            app.UseAuthentication();

            app.UseLykkeConfiguration(options =>
            {
                options.SwaggerOptions = _swaggerOptions;
            });
        }

    }
}
