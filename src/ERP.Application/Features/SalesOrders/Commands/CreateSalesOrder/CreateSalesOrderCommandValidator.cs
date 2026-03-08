using FluentValidation;

namespace ERP.Application.Features.SalesOrders.Commands.CreateSalesOrder;

public sealed class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.OrderNo).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CustomerCariAccountId).NotEmpty();
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
