namespace ERP.Infrastructure.Communication;

public sealed class ClaudeOptions
{
    public const string SectionName = "Claude";

    public bool Enabled { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "claude-sonnet-4-20250514";
    public int MaxTokens { get; init; } = 4096;
    public string Endpoint { get; init; } = "https://api.anthropic.com/v1/messages";
    public string ApiVersion { get; init; } = "2023-06-01";
    public int RequestTimeoutSeconds { get; init; } = 60;
}
