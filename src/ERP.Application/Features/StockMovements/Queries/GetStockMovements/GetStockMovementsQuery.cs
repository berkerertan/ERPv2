using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockMovements;

public sealed record GetStockMovementsQuery(
    string? Search = null,
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    StockMovementType? Type = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50,
    string SortDir = "desc") : IRequest<IReadOnlyList<StockMovementDto>>;
