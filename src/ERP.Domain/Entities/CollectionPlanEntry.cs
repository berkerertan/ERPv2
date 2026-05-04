using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class CollectionPlanEntry : TenantOwnedEntity
{
    public Guid CariAccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal OverdueAmount { get; set; }
    public int OverdueDays { get; set; }
    public decimal RiskUsageRate { get; set; }
    public CollectionPlanPriority Priority { get; set; } = CollectionPlanPriority.Medium;
    public CollectionPlanStatus Status { get; set; } = CollectionPlanStatus.Open;
    public DateTime? NextActionDateUtc { get; set; }
    public DateTime? PromiseDateUtc { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastContactAtUtc { get; set; }
    public string? LastContactNote { get; set; }
}
