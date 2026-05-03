namespace ERP.API.Contracts.StockMovements;

public sealed record ApplyInventoryCountRequest(
    string? ClientRequestId,
    Guid? SessionId,
    Guid WarehouseId,
    string? ReferenceNo,
    string? Notes,
    string? LocationCode,
    IReadOnlyList<ApplyInventoryCountItemRequest> Items);

public sealed record ApplyInventoryCountItemRequest(
    Guid ProductId,
    decimal CountedQuantity);

public sealed record ApplyInventoryCountResponse(
    Guid SessionId,
    string ReferenceNo,
    int SubmittedItems,
    int AppliedItems,
    int SkippedItems,
    decimal TotalIncreaseQuantity,
    decimal TotalDecreaseQuantity);

public sealed record StartInventoryCountSessionRequest(
    Guid WarehouseId,
    string? ReferenceNo,
    string? Notes,
    string? LocationCode);
