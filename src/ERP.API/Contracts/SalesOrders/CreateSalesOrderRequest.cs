namespace ERP.API.Contracts.SalesOrders;

public sealed record CreateSalesOrderItemRequest(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record CreateSalesOrderRequest(
    string OrderNo,
    Guid CustomerCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<CreateSalesOrderItemRequest> Items);
