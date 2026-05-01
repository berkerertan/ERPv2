using ERP.Domain.Enums;

namespace ERP.Application.Features.StockMovements.Queries.GetInventoryCountSessionById;

public sealed record InventoryCountSessionDetailDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    InventoryCountSessionStatus Status,
    string ReferenceNo,
    string? Notes,
    string? LocationCode,
    Guid? StartedByUserId,
    string? StartedByUserName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int SubmittedItems,
    int AppliedItems,
    int SkippedItems,
    decimal TotalIncreaseQuantity,
    decimal TotalDecreaseQuantity,
    IReadOnlyList<InventoryCountSessionItemDto> Items);

public sealed record InventoryCountSessionItemDto(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string? Barcode,
    string Unit,
    string? LocationCode,
    Guid? CountedByUserId,
    string? CountedByUserName,
    decimal SystemQuantity,
    decimal CountedQuantity,
    decimal DifferenceQuantity,
    DateTime CountedAtUtc);
