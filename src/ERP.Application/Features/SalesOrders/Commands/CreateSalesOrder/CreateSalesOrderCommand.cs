using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.CreateSalesOrder;

public sealed record CreateSalesOrderItemInput(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record CreateSalesOrderCommand(
    string OrderNo,
    Guid CustomerCariAccountId,
    Guid WarehouseId,
    IReadOnlyList<CreateSalesOrderItemInput> Items) : IRequest<Guid>;
