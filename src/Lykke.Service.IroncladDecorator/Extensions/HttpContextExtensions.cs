using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Lykke.Service.IroncladDecorator.Extensions
{
    public static class HttpContextExtensions
    {
        private const string BearerPrefix = "Bearer ";

        public static string GetBearerTokenFromAuthorizationHeader(this HttpContext context)
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
                return null;

            if (!context.Request.Headers.TryGetValue("Authorization", out var headers))
                return null;

            var authHeader = headers.FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(BearerPrefix))
                return null;

            var token = authHeader.Substring(BearerPrefix.Length);

            return token;
        }
    }
}
