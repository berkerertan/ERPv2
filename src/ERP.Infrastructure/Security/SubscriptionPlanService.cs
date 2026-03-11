using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Models;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Security;

public sealed class SubscriptionPlanService(ErpDbContext dbContext) : ISubscriptionPlanService
{
    public async Task<SubscriptionPlanConfig> GetPlanConfigAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
    {
        var setting = await dbContext.SubscriptionPlanSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Plan == plan, cancellationToken);

        if (setting is null)
        {
            return BuildFallback(plan);
        }

        return new SubscriptionPlanConfig(
            setting.Plan,
            setting.DisplayName,
            SubscriptionPlanCatalog.GetRoleForPlan(setting.Plan),
            setting.MonthlyPrice,
            setting.MaxUsers,
            ParseFeatures(setting.FeaturesCsv, setting.Plan),
            setting.IsActive);
    }

    public async Task<IReadOnlyList<SubscriptionPlanConfig>> GetAllPlansAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.SubscriptionPlanSettings
            .AsNoTracking()
            .OrderBy(x => x.Plan)
            .ToListAsync(cancellationToken);

        var result = Enum.GetValues<SubscriptionPlan>()
            .Select(plan =>
            {
                var setting = settings.FirstOrDefault(x => x.Plan == plan);
                return setting is null
                    ? BuildFallback(plan)
                    : new SubscriptionPlanConfig(
                        setting.Plan,
                        setting.DisplayName,
                        SubscriptionPlanCatalog.GetRoleForPlan(setting.Plan),
                        setting.MonthlyPrice,
                        setting.MaxUsers,
                        ParseFeatures(setting.FeaturesCsv, setting.Plan),
                        setting.IsActive);
            })
            .Where(x => !onlyActive || x.IsActive)
            .ToList();

        return result;
    }

    private static SubscriptionPlanConfig BuildFallback(SubscriptionPlan plan)
    {
        return new SubscriptionPlanConfig(
            plan,
            SubscriptionPlanCatalog.GetDisplayName(plan),
            SubscriptionPlanCatalog.GetRoleForPlan(plan),
            SubscriptionPlanCatalog.GetDefaultMonthlyPrice(plan),
            SubscriptionPlanCatalog.GetMaxUsers(plan),
            SubscriptionPlanCatalog.GetFeatures(plan),
            true);
    }

    private static IReadOnlyList<string> ParseFeatures(string featuresCsv, SubscriptionPlan plan)
    {
        var values = (featuresCsv ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count == 0 ? SubscriptionPlanCatalog.GetFeatures(plan) : values;
    }
}
