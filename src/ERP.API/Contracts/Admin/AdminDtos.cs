using ERP.Domain.Enums;

namespace ERP.API.Contracts.Admin;

public sealed record AdminOverviewDto(
    int TotalSubscribers,
    int ActiveSubscribers,
    int SuspendedSubscribers,
    int CancelledSubscribers,
    int TotalUsers,
    decimal TotalMonthlyRecurringRevenue,
    int TodayActiveUsers,
    int TodayRequestCount);

public sealed record AdminTenantListItemDto(
    Guid TenantId,
    string Name,
    string Code,
    SubscriptionPlan Plan,
    string AssignedRole,
    SubscriptionStatus Status,
    int MaxUsers,
    int CurrentUserCount,
    DateTime SubscriptionStartAtUtc,
    DateTime? SubscriptionEndAtUtc,
    DateTime? LastActivityAtUtc,
    decimal MonthlyPrice);

public sealed record AdminTenantDetailDto(
    Guid TenantId,
    string Name,
    string Code,
    SubscriptionPlan Plan,
    string AssignedRole,
    SubscriptionStatus Status,
    int MaxUsers,
    int CurrentUserCount,
    DateTime SubscriptionStartAtUtc,
    DateTime? SubscriptionEndAtUtc,
    decimal MonthlyPrice,
    IReadOnlyList<string> Features,
    IReadOnlyList<AdminActivityLogDto> RecentActivities);

public sealed record AdminActivityLogDto(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    string? UserName,
    string HttpMethod,
    string Path,
    int StatusCode,
    int DurationMs,
    DateTime OccurredAtUtc);

public sealed record AdminAuditLogDto(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    string? UserName,
    string HttpMethod,
    string Path,
    int StatusCode,
    int DurationMs,
    string? IpAddress,
    string? UserAgent,
    DateTime OccurredAtUtc);

public sealed record AdminAuditLogSummaryDto(
    int TotalCount,
    int ErrorCount,
    int TodayCount,
    int UniqueUsers,
    int UniqueTenants,
    double AverageDurationMs);

public sealed record AdminRevenuePointDto(
    string Plan,
    int SubscriberCount,
    decimal MonthlyPrice,
    decimal Revenue);

public sealed record AdminRevenueSummaryDto(
    decimal TotalMonthlyRevenue,
    IReadOnlyList<AdminRevenuePointDto> Breakdown);

public sealed class UpdateTenantSubscriptionRequest
{
    public SubscriptionPlan Plan { get; init; }
    public SubscriptionStatus Status { get; init; }
    public DateTime? SubscriptionEndAtUtc { get; init; }
}

public sealed class UpdatePlanSettingRequest
{
    public string DisplayName { get; init; } = string.Empty;
    public decimal MonthlyPrice { get; init; }
    public int MaxUsers { get; init; }
    public bool IsActive { get; init; } = true;
    public List<string> Features { get; init; } = [];
}

public sealed record LandingPageContentDto(
    string Key,
    string Title,
    string Content,
    bool IsPublished,
    int SortOrder,
    DateTime UpdatedAtUtc);

public sealed class UpdateLandingPageContentRequest
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsPublished { get; init; } = true;
    public int SortOrder { get; init; }
}
