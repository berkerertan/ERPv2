using FluentValidation;

namespace ERP.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.OrderNo).NotEmpty().MaximumLength(30);
        RuleFor(x => x.SupplierCariAccountId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
