namespace ERP.API.Contracts.Pos;

public sealed record CreatePosQuickSaleItemRequest(
    Guid? ProductId,
    string? Barcode,
    decimal Quantity,
    decimal? UnitPrice);

public sealed record CreatePosQuickSaleRequest(
    Guid CustomerCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<CreatePosQuickSaleItemRequest> Items,
    string? Note);
