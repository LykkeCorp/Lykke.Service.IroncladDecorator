using System;
using Lykke.Service.Session.Client;
using MessagePack;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    [MessagePackObject(true)]
    public class UserSession
    {
        public string Id { get; set; }
        public string OldLykkeToken { get; set; }
        public string LykkeClientId { get; set; }
        public Guid AuthId { get; set; }
        public TokenData IroncladTokenResponse { get; set; }
        public string AuthorizeQuery { get; set; }

        public UserSession()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        public void SaveAuthResult(IClientSession clientSession, TokenData tokens)
        {
            OldLykkeToken = clientSession.SessionToken;
            AuthId = clientSession.AuthId;
            LykkeClientId  = clientSession.ClientId;
            IroncladTokenResponse = tokens;
        }
    }
}
