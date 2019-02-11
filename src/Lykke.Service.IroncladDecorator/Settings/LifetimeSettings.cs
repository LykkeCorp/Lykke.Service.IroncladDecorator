using System;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class LifetimeSettings
    {
        public TimeSpan UserSessionCookieLifetime { get; set; }

        public LifetimeSettings()
        {
            UserSessionCookieLifetime = TimeSpan.FromDays(25);
        }
    }
}
