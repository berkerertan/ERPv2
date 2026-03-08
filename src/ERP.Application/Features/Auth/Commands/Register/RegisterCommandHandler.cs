using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Models;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterCommand, UserRegistrationResponse>
{
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

        var role = NormalizeRole(request.Role);

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = role
        };

        await userRepository.AddAsync(user, cancellationToken);

        return new UserRegistrationResponse(user.Id, user.UserName, user.Role);
    }

    private static string NormalizeRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || role.Equals(AppRoles.Employee, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoles.Employee;
        }

        if (role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return AppRoles.Admin;
        }

        throw new ConflictException($"Invalid role '{role}'. Allowed roles: {AppRoles.Admin}, {AppRoles.Employee}.");
    }
}
