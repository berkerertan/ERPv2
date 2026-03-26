using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastActiveUtc { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; }
}
