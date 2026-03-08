using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.UpdateSalesOrder;

public sealed record UpdateSalesOrderItemInput(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record UpdateSalesOrderCommand(
    Guid SalesOrderId,
    string OrderNo,
    Guid CustomerCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<UpdateSalesOrderItemInput> Items) : IRequest;
