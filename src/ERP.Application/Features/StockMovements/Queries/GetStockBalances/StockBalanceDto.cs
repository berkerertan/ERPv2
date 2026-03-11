namespace ERP.Application.Features.StockMovements.Queries.GetStockBalances;

public sealed record StockReportItemDto(
    Guid ProductId,
    string ProductName,
    string Barcode,
    string WarehouseName,
    decimal Balance,
    string Unit,
    decimal TotalValue);
