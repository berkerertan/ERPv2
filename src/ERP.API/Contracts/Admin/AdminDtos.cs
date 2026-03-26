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

public sealed record AdminUserListItemDto(
    Guid UserId,
    string UserName,
    string? Email,
    string Role,
    Guid? TenantId,
    string? TenantName,
    string? TenantCode,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);

public sealed record AdminUserDetailDto(
    Guid UserId,
    string UserName,
    string? Email,
    string Role,
    Guid? TenantId,
    string? TenantName,
    string? TenantCode,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    SubscriptionPlan? SubscriptionPlan,
    SubscriptionStatus? SubscriptionStatus,
    IReadOnlyList<AdminActivityLogDto> RecentActivities);

public sealed record AdminSystemHealthOverviewDto(
    string Status,
    DateTime CurrentUtc,
    DateTime StartedAtUtc,
    double UptimeSeconds,
    string Environment,
    string Version,
    bool AuthorizationEnforced,
    bool DatabaseReachable,
    int RequestsLastHour,
    int ErrorsLastHour,
    double ErrorRateLastHour,
    double AverageDurationMsLastHour,
    int ActiveUsersToday,
    int ActiveTenantsToday,
    DateTime? LastRequestAtUtc,
    DateTime? LastErrorAtUtc);

public sealed record AdminSystemDependencyStatusDto(
    string Name,
    string Status,
    long ResponseTimeMs,
    string? Message,
    DateTime CheckedAtUtc);

public sealed record AdminSystemHealthTimelinePointDto(
    DateTime BucketStartUtc,
    int RequestCount,
    int ErrorCount,
    double AverageDurationMs);

public sealed record AdminSystemHealthTimelineDto(
    int RangeMinutes,
    int BucketMinutes,
    IReadOnlyList<AdminSystemHealthTimelinePointDto> Points);

public sealed record AdminEmailTemplateDto(
    Guid Id,
    string Key,
    string Name,
    string SubjectTemplate,
    string BodyTemplate,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed class UpsertAdminEmailTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public string SubjectTemplate { get; init; } = string.Empty;
    public string BodyTemplate { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class SendTenantEmailRequest
{
    public string TemplateKey { get; init; } = "welcome";
    public List<Guid> TenantIds { get; init; } = [];
    public bool SendToAllActiveTenants { get; init; }
    public bool SendToAllTenantUsers { get; init; }
    public string? SubjectOverride { get; init; }
    public string? BodyOverride { get; init; }
    public Dictionary<string, string> Variables { get; init; } = [];
}

public sealed record AdminTenantEmailDispatchItemDto(
    Guid? TenantId,
    string? TenantCode,
    string? TenantName,
    string RecipientEmail,
    string Status,
    string Message);

public sealed record AdminTenantEmailDispatchResultDto(
    int RequestedTenantCount,
    int ProcessedTenantCount,
    int SentCount,
    int FailedCount,
    int SkippedCount,
    IReadOnlyList<AdminTenantEmailDispatchItemDto> Items);

public sealed record AdminEmailDispatchLogDto(
    Guid Id,
    Guid? CampaignId,
    Guid? TenantId,
    string? TenantCode,
    string? TenantName,
    string TemplateKey,
    string RecipientEmail,
    string Subject,
    string Status,
    string? ProviderMessage,
    DateTime AttemptedAtUtc,
    DateTime? SentAtUtc,
    Guid? TriggeredByUserId,
    string? TriggeredByUserName);

public sealed class AdminEmailCampaignDraftRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string TemplateKey { get; init; } = "welcome";
    public List<Guid> TenantIds { get; init; } = [];
    public bool SendToAllActiveTenants { get; init; }
    public bool SendToAllTenantUsers { get; init; }
    public string? SubjectOverride { get; init; }
    public string? BodyOverride { get; init; }
    public Dictionary<string, string> Variables { get; init; } = [];
    public DateTime? ScheduledAtUtc { get; init; }
    public bool IsHtml { get; init; } = true;
}

public sealed class AdminEmailCampaignPreviewRequest
{
    public string TemplateKey { get; init; } = "welcome";
    public List<Guid> TenantIds { get; init; } = [];
    public bool SendToAllActiveTenants { get; init; }
    public bool SendToAllTenantUsers { get; init; }
}

public sealed record AdminEmailCampaignAudienceRecipientDto(
    Guid? TenantId,
    string? TenantCode,
    string? TenantName,
    string RecipientEmail);

public sealed record AdminEmailCampaignAudiencePreviewDto(
    int TenantCount,
    int RecipientCount,
    IReadOnlyList<AdminEmailCampaignAudienceRecipientDto> RecipientsSample);

public sealed record AdminEmailCampaignDto(
    Guid Id,
    string Name,
    string? Description,
    string TemplateKey,
    string SubjectTemplate,
    string BodyTemplate,
    bool IsHtml,
    bool SendToAllActiveTenants,
    bool SendToAllTenantUsers,
    IReadOnlyList<Guid> TenantIds,
    Dictionary<string, string> Variables,
    PlatformEmailCampaignStatus Status,
    DateTime? ScheduledAtUtc,
    DateTime? QueuedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    int SkippedCount,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record AdminEmailCampaignQueueResultDto(
    Guid CampaignId,
    PlatformEmailCampaignStatus Status,
    int TotalRecipients,
    DateTime? ScheduledAtUtc);

public sealed record AdminEmailCampaignRecipientDto(
    Guid Id,
    Guid CampaignId,
    Guid? TenantId,
    string? TenantCode,
    string? TenantName,
    string RecipientEmail,
    PlatformEmailRecipientStatus Status,
    int AttemptCount,
    DateTime? NextAttemptAtUtc,
    DateTime? LastAttemptedAtUtc,
    DateTime? SentAtUtc,
    string? ProviderMessage);

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
