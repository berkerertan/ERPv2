namespace ERP.Infrastructure.Communication;

public sealed class AccountEmailOptions
{
    public const string SectionName = "AccountEmail";

    public string ProductName { get; init; } = "StokNet";
    public string FrontendBaseUrl { get; init; } = "http://localhost:4200";
    public string VerifyEmailPath { get; init; } = "/auth/verify-email";
}
