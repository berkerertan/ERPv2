using ERP.Application.Common.Models;
using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Security;

public interface IJwtTokenService
{
    TokenResult GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
}
