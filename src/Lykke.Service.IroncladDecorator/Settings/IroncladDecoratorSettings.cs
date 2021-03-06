﻿using JetBrains.Annotations;

namespace Lykke.Service.IroncladDecorator.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class IroncladDecoratorSettings
    {
        public DbSettings Db { get; set; }
        public LifetimeSettings LifetimeSettings { get; set; }
        public IroncladSettings IroncladSettings { get; set; }
        public ValidationSettings ValidationSettings { get; set; }
        public CorsSettings CorsSettings { get; set; }
    }
}
