using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.DeleteSalesOrder;

public sealed record DeleteSalesOrderCommand(Guid SalesOrderId) : IRequest;
