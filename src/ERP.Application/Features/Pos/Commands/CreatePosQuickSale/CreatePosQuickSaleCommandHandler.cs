using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.Pos.Commands.CreatePosQuickSale;

public sealed class CreatePosQuickSaleCommandHandler(
    ISalesOrderRepository salesOrderRepository,
    ICariAccountRepository cariAccountRepository,
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository)
    : IRequestHandler<CreatePosQuickSaleCommand, PosQuickSaleResult>
{
    public async Task<PosQuickSaleResult> Handle(CreatePosQuickSaleCommand request, CancellationToken cancellationToken)
    {
        var buyerBch = await cariAccountRepository.GetByIdAsync(request.CustomerCariAccountId, cancellationToken)
            ?? throw new NotFoundException("Buyer/BCH cari account not found.");

        if (buyerBch.Type == CariType.Supplier)
        {
            throw new ConflictException("Selected cari account is not a buyer/BCH account.");
        }

        if (await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Warehouse not found.");
        }

        var resolvedItems = new List<ResolvedPosSaleItem>();

        foreach (var item in request.Items)
        {
            var product = await ResolveProductAsync(item, productRepository, cancellationToken)
                ?? throw new NotFoundException("Product not found for POS item.");

            var unitPrice = item.UnitPrice ?? product.DefaultSalePrice;
            if (unitPrice < 0)
            {
                throw new ConflictException("Unit price cannot be negative.");
            }

            resolvedItems.Add(new ResolvedPosSaleItem(product, item.Quantity, unitPrice));
        }

        foreach (var grouped in resolvedItems.GroupBy(x => x.Product.Id))
        {
            var requestedQuantity = grouped.Sum(x => x.Quantity);
            var available = await stockMovementRepository.GetCurrentQuantityAsync(request.WarehouseId, grouped.Key, cancellationToken);
            if (available < requestedQuantity)
            {
                var productName = grouped.First().Product.Name;
                throw new ConflictException($"Insufficient stock for product '{productName}'. Available: {available}, requested: {requestedQuantity}.");
            }
        }

        var orderNo = await GenerateUniqueOrderNoAsync(salesOrderRepository, cancellationToken);
        var saleDateUtc = DateTime.UtcNow;

        var order = new SalesOrder
        {
            OrderNo = orderNo,
            CustomerCariAccountId = request.CustomerCariAccountId,
            WarehouseId = request.WarehouseId,
            Status = OrderStatus.Approved,
            OrderDateUtc = saleDateUtc,
            Items = resolvedItems.Select(item => new SalesOrderItem
            {
                ProductId = item.Product.Id,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        await salesOrderRepository.AddAsync(order, cancellationToken);

        foreach (var item in resolvedItems)
        {
            var movement = new StockMovement
            {
                WarehouseId = request.WarehouseId,
                ProductId = item.Product.Id,
                Type = StockMovementType.Out,
                Reason = StockMovementReason.PosSale,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ReferenceNo = order.OrderNo,
                MovementDateUtc = saleDateUtc
            };

            await stockMovementRepository.AddAsync(movement, cancellationToken);
        }

        var totalAmount = resolvedItems.Sum(x => x.Quantity * x.UnitPrice);
        buyerBch.CurrentBalance += totalAmount;
        await cariAccountRepository.UpdateAsync(buyerBch, cancellationToken);

        return new PosQuickSaleResult(
            order.Id,
            order.OrderNo,
            totalAmount,
            resolvedItems.Count,
            saleDateUtc);
    }

    private static async Task<Product?> ResolveProductAsync(
        PosQuickSaleItemInput item,
        IProductRepository productRepository,
        CancellationToken cancellationToken)
    {
        if (item.ProductId.HasValue)
        {
            return await productRepository.GetByIdAsync(item.ProductId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(item.Barcode))
        {
            var barcode = item.Barcode.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty);
            return await productRepository.GetByBarcodeAsync(barcode, cancellationToken);
        }

        return null;
    }

    private static async Task<string> GenerateUniqueOrderNoAsync(
        ISalesOrderRepository salesOrderRepository,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20; i++)
        {
            var candidate = $"POS-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            if (await salesOrderRepository.GetByOrderNoAsync(candidate, cancellationToken) is null)
            {
                return candidate;
            }

            await Task.Delay(10, cancellationToken);
        }

        throw new ConflictException("Could not generate unique POS order number.");
    }

    private sealed record ResolvedPosSaleItem(Product Product, decimal Quantity, decimal UnitPrice);
}
