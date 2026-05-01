using ERP.Domain.Enums;

namespace ERP.Application.Features.StockMovements.Queries.GetInventoryCountSessions;

public sealed record InventoryCountSessionListItemDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    InventoryCountSessionStatus Status,
    string ReferenceNo,
    string? Notes,
    string? LocationCode,
    string? StartedByUserName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int SubmittedItems,
    int AppliedItems,
    int SkippedItems,
    decimal TotalIncreaseQuantity,
    decimal TotalDecreaseQuantity);
