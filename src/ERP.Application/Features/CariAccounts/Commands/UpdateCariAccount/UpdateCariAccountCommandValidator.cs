using FluentValidation;

namespace ERP.Application.Features.CariAccounts.Commands.UpdateCariAccount;

public sealed class UpdateCariAccountCommandValidator : AbstractValidator<UpdateCariAccountCommand>
{
    public UpdateCariAccountCommandValidator()
    {
        RuleFor(x => x.CariAccountId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .Matches(@"^[0-9+\-() ]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.RiskLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaturityDays).InclusiveBetween(0, 365);
    }
}
