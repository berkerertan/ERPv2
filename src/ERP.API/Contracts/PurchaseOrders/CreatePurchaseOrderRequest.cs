namespace ERP.API.Contracts.PurchaseOrders;

public sealed record CreatePurchaseOrderItemRequest(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record CreatePurchaseOrderRequest(
    string OrderNo,
    Guid SupplierCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<CreatePurchaseOrderItemRequest> Items);
