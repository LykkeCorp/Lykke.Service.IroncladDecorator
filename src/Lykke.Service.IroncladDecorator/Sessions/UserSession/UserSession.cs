using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lykke.Service.Session.Client;
using Newtonsoft.Json;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public class UserSession
    {
        public string Id { get; }

        public string OldLykkeToken => Get<string>(_oldLykkeToken);
        public string LykkeClientId => Get<string>(_lykkeClientId);
        public string AuthId => Get<string>(_authId);
        public string AuthorizeQuery
        {
            get => Get<string>(_authorizeQueryString);
            set => Set(_authorizeQueryString, value);
        }

        private const string _authorizeQueryString = "AuthorizeQueryString";
        private const string _lykkeClientId = "LykkeClientId";
        private const string _authId = "AuthId";
        private const string _oldLykkeToken = "OldLykkeToken";
        private const string _ironcladTokenResponse = "IroncladTokenResponse";

        public IReadOnlyDictionary<string, string> Data => new ReadOnlyDictionary<string, string>(_data);
        private readonly IDictionary<string, string> _data;

        public UserSession(string id, IDictionary<string, string> data)
        {
            Id = id;
            _data = data;
        }

        public UserSession()
        {
            Id = Guid.NewGuid().ToString("N");
            _data = new Dictionary<string, string>();
        }

        private void Set<T>(string key, T value)
        {
            _data[key] = JsonConvert.SerializeObject(value);
        }

        private T Get<T>(string key)
        {
            _data.TryGetValue(key, out var value);

            var deserialized = JsonConvert.DeserializeObject<T>(value);

            return deserialized;
        }

        public void SaveAuthResult(IClientSession clientSession, TokenData tokens)
        {
            Set(_oldLykkeToken, clientSession.SessionToken);
            Set(_authId, clientSession.AuthId);
            Set(_lykkeClientId, clientSession.ClientId);
            Set(_ironcladTokenResponse, tokens);
        }
    }
}
