using ERP.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ERP.API.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePlatformAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var role = user.FindFirstValue(ClaimTypes.Role);
        var tenantId = user.FindFirstValue("tenant_id");

        if (!string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(tenantId))
        {
            context.Result = new ForbidResult();
        }
    }
}
