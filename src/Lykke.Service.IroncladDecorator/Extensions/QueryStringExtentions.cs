using Microsoft.AspNetCore.Http;

namespace Lykke.Service.IroncladDecorator.Extensions
{
    internal static class QueryStringExtentions
    {
        public static string ToFragmentString(this QueryString queryString)
        {
            var uriComponent = queryString.ToUriComponent();
            var substring = uriComponent.Substring(1);
            return $"#{substring}";
        }
    }
}
