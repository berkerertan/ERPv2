namespace ERP.API.Contracts.StockMovements;

public sealed record TransferStockRequest(
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    string? ReferenceNo);
