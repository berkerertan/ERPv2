using FluentValidation;

namespace ERP.Application.Features.StockMovements.Commands.CreateStockMovement;

public sealed class CreateStockMovementCommandValidator : AbstractValidator<CreateStockMovementCommand>
{
    public CreateStockMovementCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).IsInEnum().When(x => x.Reason.HasValue);
        RuleFor(x => x.ReasonNote).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.ReasonNote));
        RuleFor(x => x.ProofImageUrl).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.ProofImageUrl));
        RuleFor(x => x.ProofImagePublicId).MaximumLength(300).When(x => !string.IsNullOrWhiteSpace(x.ProofImagePublicId));
    }
}
