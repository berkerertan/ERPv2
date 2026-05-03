using FluentValidation;

namespace ERP.Application.Features.SalesOrders.Commands.RejectSalesOrder;

public sealed class RejectSalesOrderCommandValidator : AbstractValidator<RejectSalesOrderCommand>
{
    public RejectSalesOrderCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CancelledByUserName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.CancelledByUserName));
    }
}
