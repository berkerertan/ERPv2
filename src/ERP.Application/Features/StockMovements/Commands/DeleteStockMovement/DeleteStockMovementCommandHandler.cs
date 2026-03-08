using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.DeleteStockMovement;

public sealed class DeleteStockMovementCommandHandler(IStockMovementRepository stockMovementRepository)
    : IRequestHandler<DeleteStockMovementCommand>
{
    public async Task Handle(DeleteStockMovementCommand request, CancellationToken cancellationToken)
    {
        if (await stockMovementRepository.GetByIdAsync(request.StockMovementId, cancellationToken) is null)
        {
            throw new NotFoundException("Stock movement not found.");
        }

        await stockMovementRepository.DeleteAsync(request.StockMovementId, cancellationToken);
    }
}
