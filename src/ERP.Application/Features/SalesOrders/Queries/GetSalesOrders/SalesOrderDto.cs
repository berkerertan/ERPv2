using ERP.Domain.Enums;

namespace ERP.Application.Features.SalesOrders.Queries.GetSalesOrders;

public sealed record SalesOrderItemDto(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record SalesOrderDto(
    Guid Id,
    string OrderNo,
    Guid CustomerCariAccountId,
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
    IReadOnlyList<SalesOrderItemDto> Items);
