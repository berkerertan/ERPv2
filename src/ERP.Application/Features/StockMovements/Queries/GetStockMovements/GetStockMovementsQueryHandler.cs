using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler(IStockMovementRepository stockMovementRepository)
    : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    public async Task<IReadOnlyList<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var movements = await stockMovementRepository.GetAllAsync(cancellationToken);

        return movements
            .OrderByDescending(x => x.MovementDateUtc)
            .Select(x => new StockMovementDto(
                x.Id,
                x.WarehouseId,
                x.ProductId,
                x.Type,
                x.Quantity,
                x.UnitPrice,
                x.MovementDateUtc,
                x.ReferenceNo))
            .ToList();
    }
}
