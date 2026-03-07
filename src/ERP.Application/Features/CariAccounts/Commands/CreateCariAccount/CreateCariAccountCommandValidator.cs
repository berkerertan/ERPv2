using FluentValidation;

namespace ERP.Application.Features.CariAccounts.Commands.CreateCariAccount;

public sealed class CreateCariAccountCommandValidator : AbstractValidator<CreateCariAccountCommand>
{
    public CreateCariAccountCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RiskLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaturityDays).InclusiveBetween(0, 365);
    }
}
