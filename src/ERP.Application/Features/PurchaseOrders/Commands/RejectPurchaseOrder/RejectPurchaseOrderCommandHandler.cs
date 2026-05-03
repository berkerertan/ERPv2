using ERP.Application.Abstractions.Auditing;
using ERP.Application.Abstractions.Notifications;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.RejectPurchaseOrder;

public sealed class RejectPurchaseOrderCommandHandler(
    IPurchaseOrderRepository purchaseOrderRepository,
    IBusinessActivityService businessActivityService,
    IUserNotificationService userNotificationService)
    : IRequestHandler<RejectPurchaseOrderCommand>
{
    public async Task Handle(RejectPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await purchaseOrderRepository.GetWithItemsAsync(request.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new ConflictException("Only draft purchase orders can be rejected.");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAtUtc = DateTime.UtcNow;
        order.CancelledByUserId = request.CancelledByUserId;
        order.CancelledByUserName = NormalizeText(request.CancelledByUserName, 100);
        order.CancellationReason = NormalizeText(request.Reason, 500);

        await purchaseOrderRepository.UpdateAsync(order, cancellationToken);

        await businessActivityService.LogAsync(
            new BusinessActivityLogEntry(
                order.TenantAccountId,
                request.CancelledByUserId,
                request.CancelledByUserName,
                "REJECT",
                $"/purchase-orders/{order.Id}",
                $"{order.OrderNo} numarali satin alma siparisi reddedildi. Neden: {order.CancellationReason}."),
            cancellationToken);

        await userNotificationService.PublishAsync(
            "warning",
            "Satin alma siparisi reddedildi",
            $"{order.OrderNo} numarali siparis reddedildi. Neden: {order.CancellationReason}.",
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
