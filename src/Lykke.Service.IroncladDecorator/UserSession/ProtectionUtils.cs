using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace Lykke.Service.IroncladDecorator.UserSession
{
    internal static class ProtectionUtils
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
            catch (CryptographicException)
            {
                return default;
            }
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using (var sha256Hash = SHA256.Create())
            {
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                var builder = new StringBuilder();
                foreach (var t in bytes)
                {
                    builder.Append(t.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
