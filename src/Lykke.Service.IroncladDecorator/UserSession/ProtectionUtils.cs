using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Lykke.Service.IroncladDecorator.UserSession
{
    public static class ProtectionUtils
    {
        public static string SerializeAndProtect<T>(T value, IDataProtector dataProtector)
        {
            var serialized = JsonConvert.SerializeObject(value);

            return dataProtector.Protect(serialized);
        }

        public static T DeserializeAndUnprotect<T>(string value, IDataProtector dataProtector)
        {
            try
            {
                var unprotected = dataProtector.Unprotect(value);

                return JsonConvert.DeserializeObject<T>(unprotected);
            }
            catch (CryptographicException e)
            {
                return default;
            }

        }
    }
}
