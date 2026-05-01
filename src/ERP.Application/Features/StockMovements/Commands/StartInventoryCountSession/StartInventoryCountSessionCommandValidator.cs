using FluentValidation;

namespace ERP.Application.Features.StockMovements.Commands.StartInventoryCountSession;

public sealed class StartInventoryCountSessionCommandValidator : AbstractValidator<StartInventoryCountSessionCommand>
{
    public StartInventoryCountSessionCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.ReferenceNo));
        RuleFor(x => x.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Notes));
        RuleFor(x => x.LocationCode).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.LocationCode));
        RuleFor(x => x.StartedByUserName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.StartedByUserName));
    }
}
