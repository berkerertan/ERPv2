namespace ERP.API.Common;

public sealed class EmailCampaignOptions
{
    public const string SectionName = "EmailCampaign";

    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 15;
    public int BatchSize { get; init; } = 100;
    public int MaxAttempts { get; init; } = 3;
    public int RetryDelayMinutes { get; init; } = 10;
}
