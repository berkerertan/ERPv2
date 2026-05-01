using FluentValidation;

namespace ERP.Application.Features.StockMovements.Commands.ApplyInventoryCount;

public sealed class ApplyInventoryCountCommandValidator : AbstractValidator<ApplyInventoryCountCommand>
{
    public ApplyInventoryCountCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().When(x => x.SessionId.HasValue);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.ReferenceNo));
        RuleFor(x => x.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Notes));
        RuleFor(x => x.LocationCode).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.LocationCode));
        RuleFor(x => x.StartedByUserName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.StartedByUserName));
        RuleFor(x => x.Items).NotEmpty();

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0);
        });
    }
}
