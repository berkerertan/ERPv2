using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Models;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.BootstrapAdmin;

public sealed class BootstrapAdminCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<BootstrapAdminCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(BootstrapAdminCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.AnyAsync(cancellationToken))
        {
            throw new ConflictException("Bootstrap is available only before the first user is created.");
        }

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = AppRoles.Admin
        };

        user.RefreshToken = jwtTokenService.GenerateRefreshToken();
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);

        await userRepository.AddAsync(user, cancellationToken);

        var token = jwtTokenService.GenerateAccessToken(user);

        return new AuthResponse(
            token.AccessToken,
            user.RefreshToken,
            token.ExpiresAtUtc,
            user.Role,
            user.UserName);
    }
}
