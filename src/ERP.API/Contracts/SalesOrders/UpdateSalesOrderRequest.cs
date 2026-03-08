namespace ERP.API.Contracts.SalesOrders;

public sealed record UpdateSalesOrderItemRequest(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record UpdateSalesOrderRequest(
    string OrderNo,
    Guid CustomerCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<UpdateSalesOrderItemRequest> Items);
