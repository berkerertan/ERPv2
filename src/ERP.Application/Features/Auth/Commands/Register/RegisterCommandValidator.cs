using ERP.Domain.Constants;
using FluentValidation;

namespace ERP.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(IsAllowedRole)
            .WithMessage($"Role must be '{AppRoles.Admin}' or '{AppRoles.Employee}'.");
    }

    private static bool IsAllowedRole(string role)
    {
        return role.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            || role.Equals(AppRoles.Employee, StringComparison.OrdinalIgnoreCase);
    }
}
