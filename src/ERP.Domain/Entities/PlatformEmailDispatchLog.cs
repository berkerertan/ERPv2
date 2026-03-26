using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class PlatformEmailDispatchLog : BaseEntity
{
    public Guid? CampaignId { get; set; }
    public Guid? TenantAccountId { get; set; }
    public string? TenantCode { get; set; }
    public string? TenantName { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderMessage { get; set; }
    public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public string? TriggeredByUserName { get; set; }
}
