using FluentValidation;

namespace ERP.Application.Features.FinanceMovements.Commands.CreateFinanceMovement;

public sealed class CreateFinanceMovementCommandValidator : AbstractValidator<CreateFinanceMovementCommand>
{
    public CreateFinanceMovementCommandValidator()
    {
        RuleFor(x => x.CariAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(250);
        RuleFor(x => x.ReferenceNo).MaximumLength(50);
    }
}
