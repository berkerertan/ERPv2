namespace ERP.Domain.Enums;

public enum PlatformEmailCampaignStatus
{
    Draft = 1,
    Scheduled = 2,
    Queued = 3,
    Processing = 4,
    Completed = 5,
    CompletedWithErrors = 6,
    Cancelled = 7
}
