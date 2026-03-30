namespace ERP.Infrastructure.Communication;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public bool Enabled { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gemini-2.0-flash";
    public string Endpoint { get; init; } = "https://generativelanguage.googleapis.com/v1beta/models";
    public int RequestTimeoutSeconds { get; init; } = 60;
}
