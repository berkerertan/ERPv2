namespace ERP.Application.Common.Models;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    string Role,
    string UserName);
