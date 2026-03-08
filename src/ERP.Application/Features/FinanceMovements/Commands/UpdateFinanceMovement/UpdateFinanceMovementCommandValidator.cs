using FluentValidation;

namespace ERP.Application.Features.FinanceMovements.Commands.UpdateFinanceMovement;

public sealed class UpdateFinanceMovementCommandValidator : AbstractValidator<UpdateFinanceMovementCommand>
{
    public UpdateFinanceMovementCommandValidator()
    {
        RuleFor(x => x.FinanceMovementId).NotEmpty();
        RuleFor(x => x.CariAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(250);
        RuleFor(x => x.ReferenceNo).MaximumLength(50);
    }
}
