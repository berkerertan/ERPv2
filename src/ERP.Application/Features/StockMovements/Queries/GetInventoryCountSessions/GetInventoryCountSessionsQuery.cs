using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetInventoryCountSessions;

public sealed record GetInventoryCountSessionsQuery(
    Guid? WarehouseId,
    bool IncludeCompleted = true,
    int Page = 1,
    int PageSize = 25) : IRequest<IReadOnlyList<InventoryCountSessionListItemDto>>;
