namespace ERP.API.Contracts.Auth;

public sealed record ResendVerificationEmailResponse(
    bool IsSent,
    string Message,
    bool IsRateLimited = false,
    int? RetryAfterSeconds = null,
    int? RemainingDailyQuota = null);
