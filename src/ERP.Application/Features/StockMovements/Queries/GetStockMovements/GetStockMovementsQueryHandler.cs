using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler(IStockMovementRepository stockMovementRepository)
    : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    public async Task<IReadOnlyList<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var movements = await stockMovementRepository.GetAllAsync(cancellationToken);
        IEnumerable<StockMovement> query = movements;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.ReferenceNo != null && x.ReferenceNo.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (request.WarehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == request.WarehouseId.Value);
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == request.ProductId.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(x => x.Type == request.Type.Value);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(x => x.MovementDateUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(x => x.MovementDateUtc <= request.ToUtc.Value);
        }

        query = string.Equals(request.SortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.MovementDateUtc)
            : query.OrderByDescending(x => x.MovementDateUtc);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200);

        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
