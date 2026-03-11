using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class TenantAccount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Starter;
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Active;
    public DateTime SubscriptionStartAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubscriptionEndAtUtc { get; set; }
    public int MaxUsers { get; set; }
}
