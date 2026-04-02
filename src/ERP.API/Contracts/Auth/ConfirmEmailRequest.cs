namespace ERP.API.Contracts.Auth;

public sealed record ConfirmEmailRequest(
    string Email,
    string Token,
    bool ResendOnExpired = false);
