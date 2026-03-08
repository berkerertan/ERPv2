using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.DeletePurchaseOrder;

public sealed class DeletePurchaseOrderCommandHandler(IPurchaseOrderRepository purchaseOrderRepository)
    : IRequestHandler<DeletePurchaseOrderCommand>
{
    public async Task Handle(DeletePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await purchaseOrderRepository.GetByIdAsync(request.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new ConflictException("Only draft purchase orders can be deleted.");
        }

        await purchaseOrderRepository.DeleteAsync(request.PurchaseOrderId, cancellationToken);
    }
}
