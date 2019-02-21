using MessagePack;

namespace Lykke.Service.IroncladDecorator.Sessions
{ 
    [MessagePackObject(true)]
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
