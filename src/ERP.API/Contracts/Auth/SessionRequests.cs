namespace ERP.API.Contracts.Auth;

public sealed record CurrentUserDto(
    Guid UserId,
    string UserName,
    string Email,
    string Role,
    Guid? TenantId,
    string? TenantName,
    string? TenantCode,
    bool IsPlatformAdmin,
    string? SubscriptionPlan,
    string? SubscriptionStatus,
    IReadOnlyList<string> Features);

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class LogoutRequest
{
    public string? RefreshToken { get; init; }
}
