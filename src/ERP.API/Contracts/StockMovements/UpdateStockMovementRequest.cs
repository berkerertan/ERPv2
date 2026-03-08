using ERP.Domain.Enums;

namespace ERP.API.Contracts.StockMovements;

public sealed record UpdateStockMovementRequest(
    Guid WarehouseId,
    Guid ProductId,
    StockMovementType Type,
    decimal Quantity,
    decimal UnitPrice,
    string? ReferenceNo);
