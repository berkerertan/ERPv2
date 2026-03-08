using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Queries.GetSalesOrders;

public sealed class GetSalesOrdersQueryHandler(ISalesOrderRepository salesOrderRepository)
    : IRequestHandler<GetSalesOrdersQuery, IReadOnlyList<SalesOrderDto>>
{
    public async Task<IReadOnlyList<SalesOrderDto>> Handle(GetSalesOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await salesOrderRepository.GetAllWithItemsAsync(cancellationToken);

        return orders
            .Select(order => new SalesOrderDto(
                order.Id,
                order.OrderNo,
                order.CustomerCariAccountId,
                order.WarehouseId,
                order.Status,
                order.OrderDateUtc,
                order.Items.Sum(item => item.Quantity * item.UnitPrice),
                order.Items.Select(item => new SalesOrderItemDto(item.ProductId, item.Quantity, item.UnitPrice)).ToList()))
            .ToList();
    }
}
