using ERP.Domain.Enums;

namespace ERP.Application.Features.StockMovements.Queries.GetStockMovements;

public sealed record StockMovementDto(
    Guid Id,
    Guid WarehouseId,
    Guid ProductId,
    StockMovementType Type,
    decimal Quantity,
    decimal UnitPrice,
    DateTime MovementDateUtc,
    string? ReferenceNo);
