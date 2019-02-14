using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Lykke.Service.IroncladDecorator.UserSession
{
    public class UserSession
    {
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

        public string Id { get; }

        public IReadOnlyDictionary<string, string> Data => new ReadOnlyDictionary<string, string>(_data);

        public void Set<T>(string key, T value)
        {
            _data[key] = JsonConvert.SerializeObject(value);
        }

        public T Get<T>(string key)
        {
            _data.TryGetValue(key, out var value);

            var deserialized = JsonConvert.DeserializeObject<T>(value);

            return deserialized;
        }

        public bool Remove(string key)
        {
            return _data.Remove(key);
        }
    }
}
