using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class SubscriptionPlanSetting : BaseEntity
{
    public SubscriptionPlan Plan { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public string FeaturesCsv { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
