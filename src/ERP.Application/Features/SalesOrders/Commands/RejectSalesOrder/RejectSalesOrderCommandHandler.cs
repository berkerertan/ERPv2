using ERP.Application.Abstractions.Auditing;
using ERP.Application.Abstractions.Notifications;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.RejectSalesOrder;

public sealed class RejectSalesOrderCommandHandler(
    ISalesOrderRepository salesOrderRepository,
    IBusinessActivityService businessActivityService,
    IUserNotificationService userNotificationService)
    : IRequestHandler<RejectSalesOrderCommand>
{
    public async Task Handle(RejectSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await salesOrderRepository.GetWithItemsAsync(request.SalesOrderId, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new ConflictException("Only draft sales orders can be rejected.");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAtUtc = DateTime.UtcNow;
        order.CancelledByUserId = request.CancelledByUserId;
        order.CancelledByUserName = NormalizeText(request.CancelledByUserName, 100);
        order.CancellationReason = NormalizeText(request.Reason, 500);

        await salesOrderRepository.UpdateAsync(order, cancellationToken);

        await businessActivityService.LogAsync(
            new BusinessActivityLogEntry(
                order.TenantAccountId,
                request.CancelledByUserId,
                request.CancelledByUserName,
                "REJECT",
                $"/sales-orders/{order.Id}",
                $"{order.OrderNo} numarali satis siparisi reddedildi. Neden: {order.CancellationReason}."),
            cancellationToken);

        await userNotificationService.PublishAsync(
            "warning",
            "Satis siparisi reddedildi",
            $"{order.OrderNo} numarali siparis reddedildi. Neden: {order.CancellationReason}.",
            "/sales-orders",
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
