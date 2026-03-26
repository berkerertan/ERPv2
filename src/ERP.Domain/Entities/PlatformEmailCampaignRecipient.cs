using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class PlatformEmailCampaignRecipient : BaseEntity
{
    public Guid CampaignId { get; set; }
    public Guid? TenantAccountId { get; set; }
    public string? TenantCode { get; set; }
    public string? TenantName { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;

    public PlatformEmailRecipientStatus Status { get; set; } = PlatformEmailRecipientStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? LastAttemptedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? ProviderMessage { get; set; }
}
