using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById;

public sealed class GetPurchaseOrderByIdQueryHandler(IPurchaseOrderRepository purchaseOrderRepository)
    : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await purchaseOrderRepository.GetWithItemsAsync(request.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        return new PurchaseOrderDto(
            order.Id,
            order.OrderNo,
            order.SupplierCariAccountId,
            order.WarehouseId,
            order.Status,
            order.OrderDateUtc,
            order.CreatedAtUtc,
            order.ApprovedAtUtc,
            order.ApprovedByUserName,
            order.CancelledAtUtc,
            order.CancelledByUserName,
            order.CancellationReason,
            order.Items.Sum(item => item.Quantity * item.UnitPrice),
            order.Items.Select(item => new PurchaseOrderItemDto(item.ProductId, item.Quantity, item.UnitPrice)).ToList());
    }
}
