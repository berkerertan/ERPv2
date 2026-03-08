using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Enums;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.ApprovePurchaseOrder;

public sealed class ApprovePurchaseOrderCommandHandler(
    IPurchaseOrderRepository purchaseOrderRepository,
    IStockMovementRepository stockMovementRepository,
    ICariAccountRepository cariAccountRepository)
    : IRequestHandler<ApprovePurchaseOrderCommand>
{
    public async Task Handle(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await purchaseOrderRepository.GetWithItemsAsync(request.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new ConflictException("Only draft purchase orders can be approved.");
        }

        var supplierCari = await cariAccountRepository.GetByIdAsync(order.SupplierCariAccountId, cancellationToken)
            ?? throw new NotFoundException("Supplier cari account not found.");

        foreach (var item in order.Items)
        {
            var movement = new StockMovement
            {
                WarehouseId = order.WarehouseId,
                ProductId = item.ProductId,
                Type = StockMovementType.In,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ReferenceNo = order.OrderNo,
                MovementDateUtc = DateTime.UtcNow
            };

            await stockMovementRepository.AddAsync(movement, cancellationToken);
        }

        var total = order.Items.Sum(x => x.Quantity * x.UnitPrice);
        supplierCari.CurrentBalance -= total;
        await cariAccountRepository.UpdateAsync(supplierCari, cancellationToken);

        order.Status = OrderStatus.Approved;
        await purchaseOrderRepository.UpdateAsync(order, cancellationToken);
    }
}
