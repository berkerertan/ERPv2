using ERP.Domain.Enums;

namespace ERP.API.Contracts.StockMovements;

public sealed record CreateStockMovementRequest(
    Guid WarehouseId,
    Guid ProductId,
    StockMovementType Type,
    decimal Quantity,
    decimal UnitPrice,
    string? ReferenceNo);
