using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class SystemActivityLog : BaseEntity
{
    public Guid? TenantAccountId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Description { get; set; }
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public int DurationMs { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
