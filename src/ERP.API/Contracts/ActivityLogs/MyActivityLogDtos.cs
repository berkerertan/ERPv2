namespace ERP.API.Contracts.ActivityLogs;

public sealed record MyActivityLogDto(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    string? UserName,
    string? Description,
    string HttpMethod,
    string Path,
    int StatusCode,
    int DurationMs,
    string? IpAddress,
    string? UserAgent,
    DateTime OccurredAtUtc);

public sealed record MyActivityLogSummaryDto(
    int TotalCount,
    int ErrorCount,
    int TodayCount,
    double AverageDurationMs,
    DateTime? LastActivityAtUtc);
