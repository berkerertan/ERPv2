using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Notifications;
using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAccountEmailService accountEmailService) : IRequestHandler<RegisterCommand, UserRegistrationResponse>
{
    private const int VerificationTokenTtlHours = 24;

    public async Task<UserRegistrationResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.GetByUserNameAsync(request.UserName, cancellationToken) is not null)
        {
            throw new ConflictException("Username already exists.");
        }

        if (await userRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
        {
            throw new ConflictException("Email already exists.");
        }

        string role;
        try
        {
            role = AppRoles.NormalizeTierRole(request.Role);
        }
        catch (ArgumentException)
        {
            throw new ConflictException($"Invalid role '{request.Role}'. Allowed roles: {AppRoles.GetPublicRoleListText()}.");
        }

        var verificationToken = EmailVerificationTokenCodec.GenerateToken();

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = role,
            IsEmailConfirmed = false,
            EmailVerificationTokenHash = EmailVerificationTokenCodec.HashToken(verificationToken),
            EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(VerificationTokenTtlHours)
        };

        await userRepository.AddAsync(user, cancellationToken);
        await accountEmailService.SendVerificationEmailAsync(user, verificationToken, cancellationToken);

        return new UserRegistrationResponse(user.Id, user.UserName, user.Role);
    }
}
