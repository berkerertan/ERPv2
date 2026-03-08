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
    decimal TotalAmount,
    IReadOnlyList<PurchaseOrderItemDto> Items);
