using ERP.Domain.Constants;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Common;

public static class SubscriptionRoleSynchronization
{
    public static async Task ApplyAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SubscriptionRoleSynchronization");

        var result = await ApplyAllAsync(dbContext, cancellationToken);
        if (!result.HasChanges)
        {
            return;
        }

        logger.LogInformation(
            "Subscription role synchronization applied. PlanSettingsUpdated: {PlanSettingsUpdated}, UsersUpdated: {UsersUpdated}",
            result.UpdatedPlans,
            result.UpdatedUsers);
    }

    public static async Task<int> ApplyTenantUserRolesAsync(ErpDbContext dbContext, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.TenantAccounts.FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return 0;
        }

        var users = await dbContext.Users
            .Where(x => x.TenantAccountId == tenantId)
            .ToListAsync(cancellationToken);

        var expectedRole = SubscriptionPlanCatalog.GetRoleForPlan(tenant.Plan);
        var updatedUsers = 0;
        var now = DateTime.UtcNow;

        foreach (var user in users)
        {
            if (string.Equals(user.Role, expectedRole, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            user.Role = expectedRole;
            user.UpdatedAtUtc = now;
            updatedUsers++;
        }

        if (updatedUsers > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return updatedUsers;
    }

    private static async Task<SyncResult> ApplyAllAsync(ErpDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var updatedPlans = 0;
        var updatedUsers = 0;

        var planSettings = await dbContext.SubscriptionPlanSettings.ToListAsync(cancellationToken);
        foreach (var setting in planSettings)
        {
            var expectedDisplayName = SubscriptionPlanCatalog.GetDisplayName(setting.Plan);
            if (!NeedsDisplayNameSync(setting.DisplayName, setting.Plan))
            {
                continue;
            }

            if (string.Equals(setting.DisplayName, expectedDisplayName, StringComparison.Ordinal))
            {
                continue;
            }

            setting.DisplayName = expectedDisplayName;
            setting.UpdatedAtUtc = now;
            updatedPlans++;
        }

        var tenantPlanLookup = await dbContext.TenantAccounts
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Plan, cancellationToken);

        var users = await dbContext.Users
            .Where(x => x.TenantAccountId.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            if (!user.TenantAccountId.HasValue || !tenantPlanLookup.TryGetValue(user.TenantAccountId.Value, out var plan))
            {
                continue;
            }

            var expectedRole = SubscriptionPlanCatalog.GetRoleForPlan(plan);
            if (string.Equals(user.Role, expectedRole, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            user.Role = expectedRole;
            user.UpdatedAtUtc = now;
            updatedUsers++;
        }

        if (updatedPlans == 0 && updatedUsers == 0)
        {
            return new SyncResult(0, 0);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SyncResult(updatedPlans, updatedUsers);
    }

    private static bool NeedsDisplayNameSync(string? displayName, SubscriptionPlan plan)
    {
        var value = (displayName ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value)
            || string.Equals(value, plan.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct SyncResult(int UpdatedPlans, int UpdatedUsers)
    {
        public bool HasChanges => UpdatedPlans > 0 || UpdatedUsers > 0;
    }
}
