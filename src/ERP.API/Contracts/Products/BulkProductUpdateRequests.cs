namespace ERP.API.Contracts.Products;

public sealed record BulkProductPriceUpdateRequest(
    IReadOnlyList<BulkProductPriceUpdateItemRequest> Items);

public sealed record BulkProductPriceUpdateItemRequest(
    Guid ProductId,
    decimal DefaultSalePrice);

public sealed record BulkProductPriceUpdateResponse(
    int Requested,
    int Updated,
    int NotFound);

public sealed record BulkProductStockUpdateRequest(
    Guid WarehouseId,
    IReadOnlyList<BulkProductStockUpdateItemRequest> Items,
    string? ReferenceNo = null,
    DateTime? MovementDateUtc = null);

public sealed record BulkProductStockUpdateItemRequest(
    Guid ProductId,
    decimal QuantityDelta,
    decimal UnitPrice);

public sealed record BulkProductStockUpdateResponse(
    int Requested,
    int MovementsCreated,
    int NotFound,
    int SkippedZeroQuantity);
