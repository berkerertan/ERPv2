using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.ApproveSalesOrder;

public sealed record ApproveSalesOrderCommand(Guid SalesOrderId) : IRequest;
