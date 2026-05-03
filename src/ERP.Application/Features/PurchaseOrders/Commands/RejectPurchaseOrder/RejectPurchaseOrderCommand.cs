using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.RejectPurchaseOrder;

public sealed record RejectPurchaseOrderCommand(
    Guid PurchaseOrderId,
    string Reason,
    Guid? CancelledByUserId,
    string? CancelledByUserName) : IRequest;
