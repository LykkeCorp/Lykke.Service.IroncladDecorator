using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.IroncladDecorator.Extensions
{
    public static class UrlHelperExtensions
    {
        public static string AbsoluteAction(
            this IUrlHelper url,
            string actionName,
            string controllerName,
            object routeValues = null)
        {
            var scheme = url.ActionContext.HttpContext.Request.Scheme;
            return url.Action(actionName, controllerName, routeValues, scheme);
        }
    }
}
