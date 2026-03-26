using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class PlatformEmailCampaign : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;

    public bool SendToAllActiveTenants { get; set; }
    public bool SendToAllTenantUsers { get; set; }
    public string TenantIdsJson { get; set; } = "[]";
    public string VariablesJson { get; set; } = "{}";

    public PlatformEmailCampaignStatus Status { get; set; } = PlatformEmailCampaignStatus.Draft;
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? QueuedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }

    public string? LastError { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
}
