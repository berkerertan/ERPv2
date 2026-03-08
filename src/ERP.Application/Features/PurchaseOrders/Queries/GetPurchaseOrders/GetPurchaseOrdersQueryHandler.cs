using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;

public sealed class GetPurchaseOrdersQueryHandler(IPurchaseOrderRepository purchaseOrderRepository)
    : IRequestHandler<GetPurchaseOrdersQuery, IReadOnlyList<PurchaseOrderDto>>
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await purchaseOrderRepository.GetAllWithItemsAsync(cancellationToken);

        return orders
            .Select(order => new PurchaseOrderDto(
                order.Id,
                order.OrderNo,
                order.SupplierCariAccountId,
                order.WarehouseId,
                order.Status,
                order.OrderDateUtc,
                order.Items.Sum(item => item.Quantity * item.UnitPrice),
                order.Items.Select(item => new PurchaseOrderItemDto(item.ProductId, item.Quantity, item.UnitPrice)).ToList()))
            .ToList();
    }
}
