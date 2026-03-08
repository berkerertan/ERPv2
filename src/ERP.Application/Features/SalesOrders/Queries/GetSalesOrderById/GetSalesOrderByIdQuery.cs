using ERP.Application.Features.SalesOrders.Queries.GetSalesOrders;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Queries.GetSalesOrderById;

public sealed record GetSalesOrderByIdQuery(Guid SalesOrderId) : IRequest<SalesOrderDto>;
