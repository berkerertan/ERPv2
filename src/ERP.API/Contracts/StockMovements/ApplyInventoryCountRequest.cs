namespace ERP.API.Contracts.StockMovements;

public sealed record ApplyInventoryCountRequest(
    Guid WarehouseId,
    string? ReferenceNo,
    string? Notes,
    IReadOnlyList<ApplyInventoryCountItemRequest> Items);

public sealed record ApplyInventoryCountItemRequest(
    Guid ProductId,
    decimal CountedQuantity);

public sealed record ApplyInventoryCountResponse(
    string ReferenceNo,
    int SubmittedItems,
    int AppliedItems,
    int SkippedItems,
    decimal TotalIncreaseQuantity,
    decimal TotalDecreaseQuantity);
