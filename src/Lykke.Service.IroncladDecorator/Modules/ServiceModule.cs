using Autofac;
using AzureStorage;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.IroncladDecorator.Clients;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
using Lykke.Service.IroncladDecorator.Sessions;
using Lykke.Service.IroncladDecorator.Settings;
using Lykke.Service.Session.Client;
using Lykke.SettingsReader;
using Microsoft.Extensions.Caching.Memory;

namespace Lykke.Service.IroncladDecorator.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => _appSettings.CurrentValue.IroncladDecoratorService.IroncladSettings);
            builder.Register(c => _appSettings.CurrentValue.IroncladDecoratorService.LifetimeSettings);
            builder.Register(c => _appSettings.CurrentValue.IroncladDecoratorService.ValidationSettings);

            builder.RegisterType<UserSessionManager>()
                .As<IUserSessionManager>()
                .SingleInstance();            
            
            builder.RegisterType<LykkeSessionManager>().As<ILykkeSessionManager>().SingleInstance();
            builder.RegisterType<OpenIdValidators>().As<IOpenIdValidators>().SingleInstance();

            RegisterRepositories(builder);

            RegisterClients(builder);
        }

        private void RegisterClients(ContainerBuilder builder)
        {
            builder.RegisterClientSessionClient(_appSettings.CurrentValue.SessionServiceClient);
            builder.RegisterLykkeServiceClient(_appSettings.CurrentValue.ClientAccountClient.ServiceUrl);
        }

        private void RegisterRepositories(ContainerBuilder builder)
        {
            builder.RegisterType<UserSessionRepository>().As<IUserSessionRepository>().SingleInstance();
            builder.RegisterType<LykkeSessionRepository>().As<ILykkeSessionRepository>().SingleInstance();

            var clientPersonalInfoConnString = _appSettings.ConnectionString(x => x.IroncladDecoratorService.Db.ClientPersonalInfoConnString);

            builder.Register(c =>
                    AzureTableStorage<ApplicationEntity>.Create(clientPersonalInfoConnString, "Applications",
                        c.Resolve<ILogFactory>()))
                .As<INoSQLTableStorage<ApplicationEntity>>()
                .SingleInstance();

            builder.RegisterType<ApplicationRepository>()
                .Named<IApplicationRepository>("notCached");

            builder.RegisterDecorator<IApplicationRepository>(
                (c, inner) => new ApplicationCachedRepository(inner, c.Resolve<IMemoryCache>()), "notCached");

        }
    }
}
