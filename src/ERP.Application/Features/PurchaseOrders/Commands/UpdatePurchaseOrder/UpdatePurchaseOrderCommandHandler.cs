using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.UpdatePurchaseOrder;

public sealed class UpdatePurchaseOrderCommandHandler(
    IPurchaseOrderRepository purchaseOrderRepository,
    ICariAccountRepository cariAccountRepository,
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository)
    : IRequestHandler<UpdatePurchaseOrderCommand>
{
    public async Task Handle(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await purchaseOrderRepository.GetWithItemsAsync(request.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new ConflictException("Only draft purchase orders can be updated.");
        }

        var existingWithOrderNo = await purchaseOrderRepository.GetByOrderNoAsync(request.OrderNo, cancellationToken);
        if (existingWithOrderNo is not null && existingWithOrderNo.Id != order.Id)
        {
            throw new ConflictException("Purchase order number already exists.");
        }

        var supplier = await cariAccountRepository.GetByIdAsync(request.SupplierCariAccountId, cancellationToken)
            ?? throw new NotFoundException("Supplier cari account not found.");

        if (supplier.Type == CariType.BuyerBch)
        {
            throw new ConflictException("Selected cari account is not a supplier.");
        }

        if (await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Warehouse not found.");
        }

        foreach (var item in request.Items)
        {
            if (await productRepository.GetByIdAsync(item.ProductId, cancellationToken) is null)
            {
                throw new NotFoundException($"Product not found: {item.ProductId}");
            }
        }

        order.OrderNo = request.OrderNo;
        order.SupplierCariAccountId = request.SupplierCariAccountId;
        order.WarehouseId = request.WarehouseId;

        order.Items.Clear();
        foreach (var item in request.Items)
        {
            order.Items.Add(new PurchaseOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        await purchaseOrderRepository.UpdateAsync(order, cancellationToken);
    }
}
