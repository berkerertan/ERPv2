using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandHandler(
    IPurchaseOrderRepository purchaseOrderRepository,
    ICariAccountRepository cariAccountRepository,
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository)
    : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (await purchaseOrderRepository.GetByOrderNoAsync(request.OrderNo, cancellationToken) is not null)
        {
            throw new ConflictException("Purchase order number already exists.");
        }

        var supplier = await cariAccountRepository.GetByIdAsync(request.SupplierCariAccountId, cancellationToken)
            ?? throw new NotFoundException("Supplier cari account not found.");

        if (supplier.Type == CariType.Customer)
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

        var order = new PurchaseOrder
        {
            OrderNo = request.OrderNo,
            SupplierCariAccountId = request.SupplierCariAccountId,
            WarehouseId = request.WarehouseId,
            Status = OrderStatus.Draft,
            OrderDateUtc = DateTime.UtcNow,
            Items = request.Items.Select(item => new PurchaseOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        await purchaseOrderRepository.AddAsync(order, cancellationToken);
        return order.Id;
    }
}
