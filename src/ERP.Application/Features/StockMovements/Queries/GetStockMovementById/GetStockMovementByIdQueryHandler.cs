using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.StockMovements.Queries.GetStockMovements;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockMovementById;

public sealed class GetStockMovementByIdQueryHandler(IStockMovementRepository stockMovementRepository)
    : IRequestHandler<GetStockMovementByIdQuery, StockMovementDto>
{
    public async Task<StockMovementDto> Handle(GetStockMovementByIdQuery request, CancellationToken cancellationToken)
    {
        var movement = await stockMovementRepository.GetByIdAsync(request.StockMovementId, cancellationToken)
            ?? throw new NotFoundException("Stock movement not found.");

        return new StockMovementDto(
            movement.Id,
            movement.WarehouseId,
            movement.ProductId,
            movement.Type,
            movement.Reason,
            movement.ReasonNote,
            movement.ProofImageUrl,
            movement.Quantity,
            movement.UnitPrice,
            movement.MovementDateUtc,
            movement.ReferenceNo);
    }
}
