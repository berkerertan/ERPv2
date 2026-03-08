using MediatR;

namespace ERP.Application.Features.SalesOrders.Queries.GetSalesOrders;

public sealed record GetSalesOrdersQuery : IRequest<IReadOnlyList<SalesOrderDto>>;
