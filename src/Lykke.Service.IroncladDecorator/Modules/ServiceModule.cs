using Autofac;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.IroncladDecorator.LykkeSession;
using Lykke.Service.IroncladDecorator.Settings;
using Lykke.Service.IroncladDecorator.UserSession;
using Lykke.Service.Session.Client;
using Lykke.SettingsReader;

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

            builder.RegisterType<UserSessionManager>().As<IUserSessionManager>().SingleInstance();            
            
            builder.RegisterType<LykkeSessionManager>().As<ILykkeSessionManager>().SingleInstance();

            builder.RegisterType<OAuthCookieManager>().As<IOAuthCookieManager>().SingleInstance();
            
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
        }
    }
}
