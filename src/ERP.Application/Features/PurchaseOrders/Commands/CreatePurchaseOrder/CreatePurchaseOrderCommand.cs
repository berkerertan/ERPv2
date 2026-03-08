using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;

public sealed record CreatePurchaseOrderItemInput(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record CreatePurchaseOrderCommand(
    string OrderNo,
    Guid SupplierCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<CreatePurchaseOrderItemInput> Items) : IRequest<Guid>;
