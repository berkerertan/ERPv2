using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Models;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ITenantAccountRepository tenantAccountRepository,
    ISubscriptionPlanService subscriptionPlanService,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUserNameAsync(request.UserName, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.TenantAccountId.HasValue && !user.IsEmailConfirmed)
        {
            throw new UnauthorizedAccessException("Email address is not verified. Please verify your email and try again.");
        }

        var tenant = user.TenantAccountId.HasValue
            ? await tenantAccountRepository.GetByIdAsync(user.TenantAccountId.Value, cancellationToken)
            : null;

        var planConfig = tenant is null
            ? null
            : await subscriptionPlanService.GetPlanConfigAsync(tenant.Plan, cancellationToken);

        user.RefreshToken = jwtTokenService.GenerateRefreshToken();
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);

        var token = jwtTokenService.GenerateAccessToken(user, tenant, planConfig?.Features);

        return new AuthResponse(
            token.AccessToken,
            user.RefreshToken,
            token.ExpiresAtUtc,
            user.Role,
            user.UserName,
            tenant?.Id,
            tenant?.Name,
            tenant?.Plan,
            tenant?.SubscriptionStatus,
            planConfig?.Features ?? [],
            token.ExpiresAtUtc,
            user.RefreshTokenExpiresAtUtc);
    }
}
