using ERP.Domain.Common;
using ERP.Domain.Constants;

namespace ERP.Domain.Entities;

public sealed class AppUser : BaseEntity
{
    public Guid? TenantAccountId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = AppRoles.Tier1;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }

    // ─── 2FA ──────────────────────────────────────────────────────
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecretKey { get; set; }

    // ─── Bildirim Tercihleri ──────────────────────────────────────
    public bool NotifEmailInvoice { get; set; } = true;
    public bool NotifEmailPayment { get; set; } = true;
    public bool NotifEmailReminder { get; set; } = true;
    public bool NotifEmailMarketing { get; set; }
    public bool NotifPushEnabled { get; set; } = true;
    public bool NotifPushOrderStatus { get; set; } = true;
    public bool NotifPushStockAlert { get; set; } = true;
}
