namespace ERP.API.Contracts.Auth;

public sealed record ConfirmEmailResponse(
    bool IsVerified,
    string Message,
    bool IsExpired = false,
    bool ResendTriggered = false,
    bool ResendSent = false,
    bool ResendRateLimited = false,
    int? RetryAfterSeconds = null);
