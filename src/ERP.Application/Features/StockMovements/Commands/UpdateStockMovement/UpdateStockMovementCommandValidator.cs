using FluentValidation;

namespace ERP.Application.Features.StockMovements.Commands.UpdateStockMovement;

public sealed class UpdateStockMovementCommandValidator : AbstractValidator<UpdateStockMovementCommand>
{
    public UpdateStockMovementCommandValidator()
    {
        RuleFor(x => x.StockMovementId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}
