namespace ERP.Application.Abstractions.Auditing;

public sealed record BusinessActivityLogEntry(
    Guid TenantAccountId,
    Guid? UserId,
    string? UserName,
    string Action,
    string Path,
    string? Description = null,
    int StatusCode = 200);
