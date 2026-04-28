using ERP.Application.Abstractions.Notifications;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.ApproveSalesOrder;

public sealed class ApproveSalesOrderCommandHandler(
    ISalesOrderRepository salesOrderRepository,
    IStockMovementRepository stockMovementRepository,
    ICariAccountRepository cariAccountRepository,
    IProductRepository productRepository,
    IUserNotificationService userNotificationService)
    : IRequestHandler<ApproveSalesOrderCommand>
{
    public async Task Handle(ApproveSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await salesOrderRepository.GetWithItemsAsync(request.SalesOrderId, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new ConflictException("Only draft sales orders can be approved.");
        }

        var buyerBchCari = await cariAccountRepository.GetByIdAsync(order.CustomerCariAccountId, cancellationToken)
            ?? throw new NotFoundException("Buyer/BCH cari account not found.");

        var availableByProduct = new Dictionary<Guid, decimal>();
        foreach (var item in order.Items)
        {
            var available = await stockMovementRepository.GetCurrentQuantityAsync(
                order.WarehouseId,
                item.ProductId,
                cancellationToken);

            availableByProduct[item.ProductId] = available;

            if (available < item.Quantity)
            {
                throw new ConflictException($"Insufficient stock for product {item.ProductId}.");
            }
        }

        foreach (var item in order.Items)
        {
            var movement = new StockMovement
            {
                WarehouseId = order.WarehouseId,
                ProductId = item.ProductId,
                Type = StockMovementType.Out,
                Reason = StockMovementReason.SalesApproval,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ReferenceNo = order.OrderNo,
                MovementDateUtc = DateTime.UtcNow
            };

            await stockMovementRepository.AddAsync(movement, cancellationToken);
        }

        var total = order.Items.Sum(x => x.Quantity * x.UnitPrice);
        buyerBchCari.CurrentBalance += total;
        await cariAccountRepository.UpdateAsync(buyerBchCari, cancellationToken);

        order.Status = OrderStatus.Approved;
        await salesOrderRepository.UpdateAsync(order, cancellationToken);

        await userNotificationService.PublishAsync(
            "success",
            "Satis siparisi onaylandi",
            $"{order.OrderNo} numarali siparis onaylandi. Musteri: {buyerBchCari.Name}. Toplam: {total:N2} TRY.",
            "/sales-orders",
            cancellationToken);

        foreach (var group in order.Items.GroupBy(x => x.ProductId))
        {
            var product = await productRepository.GetByIdAsync(group.Key, cancellationToken);
            if (product is null)
            {
                continue;
            }

            var threshold = product.CriticalStockLevel > 0
                ? product.CriticalStockLevel
                : product.MinimumStockLevel;

            if (threshold <= 0 || !availableByProduct.TryGetValue(group.Key, out var currentStock))
            {
                continue;
            }

            var remaining = currentStock - group.Sum(x => x.Quantity);
            if (remaining > threshold)
            {
                continue;
            }

            await userNotificationService.PublishAsync(
                "warning",
                "Kritik stok uyarisi",
                $"{product.Name} urunu kritik seviyeye yaklasti. Kalan stok: {remaining:N2} {product.Unit}. Esik: {threshold:N2} {product.Unit}.",
                "/products",
                cancellationToken);
        }
    }
}
