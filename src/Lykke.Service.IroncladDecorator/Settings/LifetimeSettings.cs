using System;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class LifetimeSettings
    {
        public TimeSpan UserSessionCookieLifetime { get; set; } = TimeSpan.FromDays(25);

        [Optional]
        public TimeSpan IroncladAccessTokenTimeBeforeRefresh { get; set; } = TimeSpan.FromMinutes(5);
    }
}
