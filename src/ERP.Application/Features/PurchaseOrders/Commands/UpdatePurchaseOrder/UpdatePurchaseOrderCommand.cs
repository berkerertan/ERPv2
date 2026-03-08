using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.UpdatePurchaseOrder;

public sealed record UpdatePurchaseOrderItemInput(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record UpdatePurchaseOrderCommand(
    Guid PurchaseOrderId,
    string OrderNo,
    Guid SupplierCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<UpdatePurchaseOrderItemInput> Items) : IRequest;
