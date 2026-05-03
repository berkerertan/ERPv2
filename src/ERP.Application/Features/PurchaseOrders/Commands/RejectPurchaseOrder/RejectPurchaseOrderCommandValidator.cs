using FluentValidation;

namespace ERP.Application.Features.PurchaseOrders.Commands.RejectPurchaseOrder;

public sealed class RejectPurchaseOrderCommandValidator : AbstractValidator<RejectPurchaseOrderCommand>
{
    public RejectPurchaseOrderCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CancelledByUserName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.CancelledByUserName));
    }
}
