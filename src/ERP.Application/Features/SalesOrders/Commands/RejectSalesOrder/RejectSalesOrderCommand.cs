using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.RejectSalesOrder;

public sealed record RejectSalesOrderCommand(
    Guid SalesOrderId,
    string Reason,
    Guid? CancelledByUserId,
    string? CancelledByUserName) : IRequest;
