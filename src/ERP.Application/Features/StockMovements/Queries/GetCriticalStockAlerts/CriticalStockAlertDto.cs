namespace ERP.Application.Features.StockMovements.Queries.GetCriticalStockAlerts;

public sealed record CriticalStockAlertDto(
    Guid WarehouseId,
    string WarehouseCode,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal CurrentQuantity,
    decimal CriticalStockLevel,
    decimal MissingQuantity);
