namespace ERP.API.Contracts.PurchaseOrders;

public sealed record UpdatePurchaseOrderItemRequest(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record UpdatePurchaseOrderRequest(
    string OrderNo,
    Guid SupplierCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<UpdatePurchaseOrderItemRequest> Items);
