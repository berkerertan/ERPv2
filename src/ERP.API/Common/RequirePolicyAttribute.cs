using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ERP.API.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePolicyAttribute(string policy) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!SecurityRuntime.IsAuthorizationEnforced(context.HttpContext.RequestServices))
        {
            return;
        }

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var authorizationService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var authorizationResult = await authorizationService.AuthorizeAsync(user, resource: null, policy);
        if (authorizationResult.Succeeded)
        {
            return;
        }

        context.Result = new ForbidResult();
    }
}
