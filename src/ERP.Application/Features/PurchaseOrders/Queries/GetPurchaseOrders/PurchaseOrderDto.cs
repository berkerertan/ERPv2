using ERP.Domain.Enums;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;

public sealed record PurchaseOrderItemDto(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record PurchaseOrderDto(
    Guid Id,
    string OrderNo,
    Guid SupplierCariAccountId,
    Guid WarehouseId,
    OrderStatus Status,
    DateTime OrderDateUtc,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    string? ApprovedByUserName,
    DateTime? CancelledAtUtc,
    string? CancelledByUserName,
    string? CancellationReason,
    decimal TotalAmount,
    IReadOnlyList<PurchaseOrderItemDto> Items);
