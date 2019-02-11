using JetBrains.Annotations;
using Lykke.Sdk.Settings;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.Session.Client;

namespace Lykke.Service.IroncladDecorator.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public IroncladDecoratorSettings IroncladDecoratorService { get; set; }
        public SessionServiceClientSettings SessionServiceClient { get; set; }
        public ClientAccountServiceClientSettings ClientAccountClient { get; set; }
    }
}
