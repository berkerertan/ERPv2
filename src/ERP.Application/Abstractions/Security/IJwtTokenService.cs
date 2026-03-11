using ERP.Application.Common.Models;
using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Security;

public interface IJwtTokenService
{
    TokenResult GenerateAccessToken(AppUser user, TenantAccount? tenant = null, IReadOnlyList<string>? features = null);
    string GenerateRefreshToken();
}
