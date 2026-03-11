using ERP.Domain.Constants;

namespace ERP.API.Contracts.Auth;

public sealed record RegisterRequest(string UserName, string Email, string Password, string Role = AppRoles.Tier1);
