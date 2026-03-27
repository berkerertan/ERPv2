using ERP.API.Common;
using ERP.Domain.Constants;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ERP.API.Common;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private const string TenantIdItemKey = "CurrentTenantId";
    private const string TenantCodeItemKey = "CurrentTenantCode";
    private const string PlatformAdminItemKey = "CurrentTenantIsPlatformAdmin";

    public async Task InvokeAsync(HttpContext context, ErpDbContext dbContext, IOptions<TenantResolutionOptions> options)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var resolutionOptions = options.Value;
        var authEnforced = SecurityRuntime.IsAuthorizationEnforced(context.RequestServices);

        if (IsPlatformAdminRequest(path, context, authEnforced))
        {
            context.Items[PlatformAdminItemKey] = true;
            await next(context);
            return;
        }

        var claimTenantId = context.User.FindFirstValue("tenant_id");
        if (Guid.TryParse(claimTenantId, out var parsedClaimTenantId))
        {
            context.Items[TenantIdItemKey] = parsedClaimTenantId;
            context.Items[TenantCodeItemKey] = context.User.FindFirstValue("tenant_code") ?? string.Empty;
            await next(context);
            return;
        }

        var headerTenantId = context.Request.Headers[resolutionOptions.TenantIdHeaderName].ToString();
        if (Guid.TryParse(headerTenantId, out var parsedHeaderTenantId))
        {
            var tenant = await dbContext.TenantAccounts
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == parsedHeaderTenantId, context.RequestAborted);

            if (tenant is not null)
            {
                context.Items[TenantIdItemKey] = tenant.Id;
                context.Items[TenantCodeItemKey] = tenant.Code;
            }

            await next(context);
            return;
        }

        var tenantCode = context.Request.Headers[resolutionOptions.TenantCodeHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(tenantCode) && resolutionOptions.EnableDevelopmentFallback)
        {
            tenantCode = resolutionOptions.DefaultTenantCode;
        }

        if (!string.IsNullOrWhiteSpace(tenantCode))
        {
            if (tenantCode.Length > 100)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid tenant identifier.");
                return;
            }

            var normalizedCode = tenantCode.Trim().ToLowerInvariant();
            var tenant = await dbContext.TenantAccounts
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code.ToLower() == normalizedCode, context.RequestAborted);

            if (tenant is not null)
            {
                context.Items[TenantIdItemKey] = tenant.Id;
                context.Items[TenantCodeItemKey] = tenant.Code;
            }
        }

        await next(context);
    }

    private static bool IsPlatformAdminRequest(string path, HttpContext context, bool authEnforced)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && string.Equals(context.User.FindFirstValue(ClaimTypes.Role), AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(context.User.FindFirstValue("tenant_id")))
        {
            return true;
        }

        return false;
    }
}
