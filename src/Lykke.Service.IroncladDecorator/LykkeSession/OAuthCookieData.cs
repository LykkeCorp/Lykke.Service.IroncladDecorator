namespace Lykke.Service.IroncladDecorator.LykkeSession
{
    public class OAuthCookieData
    {
        public string LykkeToken { get; }
        public string LykkeUserId { get; }

        public OAuthCookieData(
            string lykkeToken,
            string lykkeUserId)
        {
            LykkeToken = lykkeToken;
            LykkeUserId = lykkeUserId;
        }
    }
}
