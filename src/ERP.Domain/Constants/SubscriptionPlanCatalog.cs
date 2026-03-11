using ERP.Domain.Enums;

namespace ERP.Domain.Constants;

public static class SubscriptionPlanCatalog
{
    public static string GetDisplayName(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Starter => "1. Kademe",
        SubscriptionPlan.Growth => "2. Kademe",
        SubscriptionPlan.Enterprise => "3. Kademe",
        _ => "1. Kademe"
    };

    public static string GetRoleForPlan(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Starter => AppRoles.Tier1,
        SubscriptionPlan.Growth => AppRoles.Tier2,
        SubscriptionPlan.Enterprise => AppRoles.Tier3,
        _ => AppRoles.Tier1
    };

    public static int GetMaxUsers(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Starter => 3,
        SubscriptionPlan.Growth => 10,
        SubscriptionPlan.Enterprise => 50,
        _ => 3
    };

    public static decimal GetDefaultMonthlyPrice(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Starter => 499m,
        SubscriptionPlan.Growth => 1499m,
        SubscriptionPlan.Enterprise => 3999m,
        _ => 499m
    };

    public static IReadOnlyList<string> GetFeatures(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Starter =>
        [
            SubscriptionFeatures.Core,
            SubscriptionFeatures.Reports
        ],
        SubscriptionPlan.Growth =>
        [
            SubscriptionFeatures.Core,
            SubscriptionFeatures.Reports,
            SubscriptionFeatures.Pos,
            SubscriptionFeatures.ExcelImport
        ],
        SubscriptionPlan.Enterprise =>
        [
            SubscriptionFeatures.Core,
            SubscriptionFeatures.Reports,
            SubscriptionFeatures.Pos,
            SubscriptionFeatures.ExcelImport,
            SubscriptionFeatures.Invoices,
            SubscriptionFeatures.AdvancedReports
        ],
        _ =>
        [
            SubscriptionFeatures.Core,
            SubscriptionFeatures.Reports
        ]
    };

    public static bool HasFeature(SubscriptionPlan plan, string feature)
    {
        return GetFeatures(plan).Any(x => string.Equals(x, feature, StringComparison.OrdinalIgnoreCase));
    }
}
