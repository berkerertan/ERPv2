using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Models;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.GetByUserNameAsync(request.UserName, cancellationToken) is not null)
        {
            throw new ConflictException("Username already exists.");
        }

        if (await userRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
        {
            throw new ConflictException("Email already exists.");
        }

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role
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
