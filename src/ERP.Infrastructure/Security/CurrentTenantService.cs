using ERP.Application.Abstractions.Security;
using ERP.Domain.Constants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ERP.Infrastructure.Security;

public sealed class CurrentTenantService(IHttpContextAccessor httpContextAccessor) : ICurrentTenantService
{
    private const string TenantIdItemKey = "CurrentTenantId";
    private const string TenantCodeItemKey = "CurrentTenantCode";
    private const string PlatformAdminItemKey = "CurrentTenantIsPlatformAdmin";

    private HttpContext? HttpContext => httpContextAccessor.HttpContext;

    public Guid? TenantId
    {
        get
        {
            var context = HttpContext;
            if (context?.Items.TryGetValue(TenantIdItemKey, out var itemValue) == true)
            {
                if (itemValue is Guid guidValue)
                {
                    return guidValue;
                }

                if (itemValue is string stringValue && Guid.TryParse(stringValue, out var parsedItemTenantId))
                {
                    return parsedItemTenantId;
                }
            }

            var claimValue = context?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claimValue, out var claimTenantId) ? claimTenantId : null;
        }
    }

    public string? TenantCode
    {
        get
        {
            var context = HttpContext;
            if (context?.Items.TryGetValue(TenantCodeItemKey, out var itemValue) == true)
            {
                return itemValue as string;
            }

            return context?.User.FindFirst("tenant_code")?.Value;
        }
    }

    public bool HasTenant => TenantId.HasValue;

    public bool IsPlatformAdmin
    {
        get
        {
            var context = HttpContext;
            if (context?.Items.TryGetValue(PlatformAdminItemKey, out var itemValue) == true && itemValue is bool flag)
            {
                return flag;
            }

            var user = context?.User;
            return user?.Identity?.IsAuthenticated == true
                   && string.Equals(user.FindFirst(ClaimTypes.Role)?.Value, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
                   && string.IsNullOrWhiteSpace(user.FindFirst("tenant_id")?.Value);
        }
    }
}
