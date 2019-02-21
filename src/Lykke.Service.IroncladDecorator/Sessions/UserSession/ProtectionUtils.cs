using System;
using System.Security.Cryptography;
using System.Text;
using MessagePack;
using Microsoft.AspNetCore.DataProtection;

namespace Lykke.Service.IroncladDecorator.Sessions
{
    public static class ProtectionUtils
    {
        public static string SerializeAndProtect<T>(T value, IDataProtector dataProtector)
        {
            var serializedBytes = MessagePackSerializer.Serialize(value);

            var protectedBytes = dataProtector.Protect(serializedBytes);

            var protectedString = Convert.ToBase64String(protectedBytes);

            return protectedString;
        }

        public static T DeserializeAndUnprotect<T>(string value, IDataProtector dataProtector)
        {
            try
            {
                var protectedBytes = Convert.FromBase64String(value);

                var unprotectedString = dataProtector.Unprotect(protectedBytes);

                var deserializedString = MessagePackSerializer.Deserialize<T>(unprotectedString);

                return deserializedString;
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
