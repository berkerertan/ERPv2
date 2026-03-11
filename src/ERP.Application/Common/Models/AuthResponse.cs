using ERP.Domain.Enums;

namespace ERP.Application.Common.Models;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    string Role,
    string UserName,
    Guid? TenantId = null,
    string? TenantName = null,
    SubscriptionPlan? SubscriptionPlan = null,
    SubscriptionStatus? SubscriptionStatus = null,
    IReadOnlyList<string>? Features = null);
