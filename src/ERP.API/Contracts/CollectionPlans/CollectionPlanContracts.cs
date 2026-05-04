using ERP.Domain.Enums;

namespace ERP.API.Contracts.CollectionPlans;

public sealed record CollectionPlanSummaryDto(
    int TotalAccountCount,
    int PlannedCount,
    int CriticalCount,
    decimal TotalOverdueAmount,
    decimal PlannedAmount);

public sealed record CollectionPlanItemDto(
    Guid CariAccountId,
    string CariCode,
    string CariName,
    decimal CurrentBalance,
    decimal RiskLimit,
    decimal OverdueAmount,
    int OverdueDays,
    decimal RiskUsageRate,
    CollectionPlanPriority SuggestedPriority,
    string SuggestedAction,
    Guid? PlanEntryId,
    string Title,
    CollectionPlanPriority Priority,
    CollectionPlanStatus Status,
    DateTime? NextActionDateUtc,
    DateTime? PromiseDateUtc,
    string? AssignedToUserName,
    string? Notes,
    DateTime? LastContactAtUtc,
    string? LastContactNote);

public sealed record CollectionPlanDashboardDto(
    CollectionPlanSummaryDto Summary,
    IReadOnlyList<CollectionPlanItemDto> Items);

public sealed class UpsertCollectionPlanRequest
{
    public Guid CariAccountId { get; init; }
    public string? Title { get; init; }
    public CollectionPlanPriority Priority { get; init; } = CollectionPlanPriority.Medium;
    public CollectionPlanStatus Status { get; init; } = CollectionPlanStatus.Open;
    public DateTime? NextActionDateUtc { get; init; }
    public DateTime? PromiseDateUtc { get; init; }
    public string? AssignedToUserName { get; init; }
    public string? Notes { get; init; }
    public string? LastContactNote { get; init; }
}

public sealed class UpdateCollectionPlanStatusRequest
{
    public CollectionPlanStatus Status { get; init; }
    public DateTime? PromiseDateUtc { get; init; }
    public string? Notes { get; init; }
    public string? LastContactNote { get; init; }
}
