using ERP.Domain.Constants;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ERP.API.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireSubscriptionFeatureAttribute(string feature) : Attribute, IAsyncAuthorizationFilter
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

        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var tenantId = user.FindFirstValue("tenant_id");

        if (string.Equals(role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        if (!int.TryParse(user.FindFirstValue("subscription_status"), out var statusValue) ||
            (SubscriptionStatus)statusValue != SubscriptionStatus.Active)
        {
            context.Result = new ObjectResult("Subscription is not active.") { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        if (!int.TryParse(user.FindFirstValue("subscription_plan"), out var planValue))
        {
            context.Result = new ObjectResult("Subscription plan is missing.") { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        var plan = (SubscriptionPlan)planValue;
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ErpDbContext>();
        var setting = await dbContext.SubscriptionPlanSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Plan == plan);

        var features = setting is null
            ? SubscriptionPlanCatalog.GetFeatures(plan)
            : ParseFeatures(setting.FeaturesCsv, plan);

        if (!features.Any(x => string.Equals(x, feature, StringComparison.OrdinalIgnoreCase)))
        {
            context.Result = new ObjectResult($"Current plan does not include feature '{feature}'.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    private static IReadOnlyList<string> ParseFeatures(string csv, SubscriptionPlan plan)
    {
        var parsed = (csv ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return parsed.Count == 0 ? SubscriptionPlanCatalog.GetFeatures(plan) : parsed;
    }
}
