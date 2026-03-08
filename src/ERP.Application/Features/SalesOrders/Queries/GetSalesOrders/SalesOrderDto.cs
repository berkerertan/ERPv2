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
    decimal TotalAmount,
    IReadOnlyList<SalesOrderItemDto> Items);
