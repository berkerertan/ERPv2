using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.ApproveSalesOrder;

public sealed class ApproveSalesOrderCommandHandler(
    ISalesOrderRepository salesOrderRepository,
    IStockMovementRepository stockMovementRepository,
    ICariAccountRepository cariAccountRepository)
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

        foreach (var item in order.Items)
        {
            var available = await stockMovementRepository.GetCurrentQuantityAsync(
                order.WarehouseId,
                item.ProductId,
                cancellationToken);

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
    }
}
