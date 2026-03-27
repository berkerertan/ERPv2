using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class UserNotification : TenantOwnedEntity
{
    public string Type { get; set; } = "info";   // info | success | warning | danger
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? Link { get; set; }
}
