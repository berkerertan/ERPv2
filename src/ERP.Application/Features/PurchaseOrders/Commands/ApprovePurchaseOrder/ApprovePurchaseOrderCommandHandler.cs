using ERP.Application.Abstractions.Auditing;
using ERP.Application.Abstractions.Notifications;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.ApprovePurchaseOrder;

public sealed class ApprovePurchaseOrderCommandHandler(
    IPurchaseOrderRepository purchaseOrderRepository,
    IStockMovementRepository stockMovementRepository,
    ICariAccountRepository cariAccountRepository,
    IBusinessActivityService businessActivityService,
    IUserNotificationService userNotificationService)
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
                Reason = StockMovementReason.PurchaseApproval,
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
        order.ApprovedAtUtc = DateTime.UtcNow;
        order.ApprovedByUserId = request.ApprovedByUserId;
        order.ApprovedByUserName = NormalizeText(request.ApprovedByUserName, 100);
        order.CancelledAtUtc = null;
        order.CancelledByUserId = null;
        order.CancelledByUserName = null;
        order.CancellationReason = null;
        await purchaseOrderRepository.UpdateAsync(order, cancellationToken);

        await businessActivityService.LogAsync(
            new BusinessActivityLogEntry(
                order.TenantAccountId,
                request.ApprovedByUserId,
                request.ApprovedByUserName,
                "APPROVE",
                $"/purchase-orders/{order.Id}",
                $"{order.OrderNo} numarali satin alma siparisi onaylandi. Tedarikci: {supplierCari.Name}. Toplam: {total:N2} TRY."),
            cancellationToken);

        await userNotificationService.PublishAsync(
            "success",
            "Satin alma siparisi onaylandi",
            $"{order.OrderNo} numarali siparis onaylandi. Tedarikci: {supplierCari.Name}. Toplam: {total:N2} TRY.",
            "/purchase-orders",
            cancellationToken);
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
