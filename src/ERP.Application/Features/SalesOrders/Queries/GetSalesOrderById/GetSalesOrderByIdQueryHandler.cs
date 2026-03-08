using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.SalesOrders.Queries.GetSalesOrders;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Queries.GetSalesOrderById;

public sealed class GetSalesOrderByIdQueryHandler(ISalesOrderRepository salesOrderRepository)
    : IRequestHandler<GetSalesOrderByIdQuery, SalesOrderDto>
{
    public async Task<SalesOrderDto> Handle(GetSalesOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await salesOrderRepository.GetWithItemsAsync(request.SalesOrderId, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        return new SalesOrderDto(
            order.Id,
            order.OrderNo,
            order.CustomerCariAccountId,
            order.WarehouseId,
            order.Status,
            order.OrderDateUtc,
            order.Items.Sum(item => item.Quantity * item.UnitPrice),
            order.Items.Select(item => new SalesOrderItemDto(item.ProductId, item.Quantity, item.UnitPrice)).ToList());
    }
}
