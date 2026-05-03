using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.ApproveSalesOrder;

public sealed record ApproveSalesOrderCommand(
    Guid SalesOrderId,
    Guid? ApprovedByUserId,
    string? ApprovedByUserName) : IRequest;
