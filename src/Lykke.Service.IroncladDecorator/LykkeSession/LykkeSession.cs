using Lykke.Service.IroncladDecorator.UserSession;

namespace Lykke.Service.IroncladDecorator.LykkeSession
{
    public class LykkeSession
    {
        public string OldLykkeToken { get; set; }
        public TokenData IroncladTokens { get; set; }

        public LykkeSession()
        {
            
        }

        public LykkeSession(string oldLykkeToken, TokenData ironcladTokens)
        {
            OldLykkeToken = oldLykkeToken;
            IroncladTokens = ironcladTokens;
        }
    }
}
