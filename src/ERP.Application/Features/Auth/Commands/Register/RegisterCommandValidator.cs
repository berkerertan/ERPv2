using ERP.Domain.Constants;
using FluentValidation;

namespace ERP.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(AppRoles.IsPublicRegistrationRole)
            .WithMessage($"Role must be one of: {AppRoles.GetPublicRoleListText()}.");
    }
}
