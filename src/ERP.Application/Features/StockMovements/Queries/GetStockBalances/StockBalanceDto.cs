namespace ERP.Application.Features.StockMovements.Queries.GetStockBalances;

public sealed record StockBalanceDto(
    Guid WarehouseId,
    string WarehouseCode,
    Guid ProductId,
    string ProductCode,
    decimal Quantity);
