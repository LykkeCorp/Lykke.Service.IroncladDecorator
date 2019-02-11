using Autofac;
using Lykke.Service.ClientAccount.Client;
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

            builder.RegisterType<UserSessionManager>()
                .WithParameter(new TypedParameter(typeof(LifetimeSettings),
                    _appSettings.CurrentValue.IroncladDecoratorService.LifetimeSettings))
                .As<IUserSessionManager>()
                .SingleInstance();

            builder.RegisterType<UserSessionRepository>().As<IUserSessionRepository>().SingleInstance();

            RegisterClients(builder);
        }

        private void RegisterClients(ContainerBuilder builder)
        {
            builder.RegisterClientSessionClient(_appSettings.CurrentValue.SessionServiceClient);
            builder.RegisterLykkeServiceClient(_appSettings.CurrentValue.ClientAccountClient.ServiceUrl);
        }
    }
}
