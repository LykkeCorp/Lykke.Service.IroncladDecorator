using System.Collections.Generic;

namespace Lykke.Service.IroncladDecorator.Settings
{
    public class ValidationSettings
    {
        public List<string> ValidIssuers { get; set; }
        public List<string> ValidAudiences { get; set; }
    }
}
