using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.DeleteSalesOrder;

public sealed class DeleteSalesOrderCommandHandler(ISalesOrderRepository salesOrderRepository)
    : IRequestHandler<DeleteSalesOrderCommand>
{
    public async Task Handle(DeleteSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await salesOrderRepository.GetByIdAsync(request.SalesOrderId, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new ConflictException("Only draft sales orders can be deleted.");
        }

        await salesOrderRepository.DeleteAsync(request.SalesOrderId, cancellationToken);
    }
}
