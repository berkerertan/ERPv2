using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetInventoryCountSessions;

public sealed class GetInventoryCountSessionsQueryHandler(
    IInventoryCountSessionRepository inventoryCountSessionRepository,
    IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetInventoryCountSessionsQuery, IReadOnlyList<InventoryCountSessionListItemDto>>
{
    public async Task<IReadOnlyList<InventoryCountSessionListItemDto>> Handle(GetInventoryCountSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await inventoryCountSessionRepository.GetFilteredAsync(
            request.WarehouseId,
            request.IncludeCompleted,
            cancellationToken);

        var warehouseNames = (await warehouseRepository.GetAllAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => x.Name);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 100);

        return sessions
            .OrderByDescending(x => x.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new InventoryCountSessionListItemDto(
                x.Id,
                x.WarehouseId,
                warehouseNames.GetValueOrDefault(x.WarehouseId, string.Empty),
                x.Status,
                x.ReferenceNo,
                x.Notes,
                x.LocationCode,
                x.StartedByUserName,
                x.StartedAtUtc,
                x.CompletedAtUtc,
                x.SubmittedItems,
                x.AppliedItems,
                x.SkippedItems,
                x.TotalIncreaseQuantity,
                x.TotalDecreaseQuantity))
            .ToList();
    }
}
