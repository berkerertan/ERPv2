namespace ERP.API.Common;

public sealed class EmailVerificationOptions
{
    public const string SectionName = "EmailVerification";

    public int TokenTtlHours { get; init; } = 24;
    public int ResendCooldownSeconds { get; init; } = 90;
    public int DailyResendLimit { get; init; } = 5;
    public bool EnforceVerifiedUsersForTenantRequests { get; init; } = true;
}
