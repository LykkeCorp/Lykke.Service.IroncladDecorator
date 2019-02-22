using System;
using Lykke.Service.IroncladDecorator.OpenIdConnect;
using Lykke.Service.Session.Client;
using MessagePack;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    [MessagePackObject(true)]
    public class UserSession
    {
        public string Id { get; set; }
        public string OldLykkeToken { get; set; }
        public string LykkeUserId { get; set; }
        public Guid AuthId { get; set; }
        public AuthorizationRequestContext AuthorizationRequestContext { get; set; }

        public UserSession()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        public void SaveAuthResult(IClientSession clientSession, TokenData tokens)
        {
            OldLykkeToken = clientSession.SessionToken;
            AuthId = clientSession.AuthId;
            LykkeUserId  = clientSession.ClientId;
        }
    }
}
