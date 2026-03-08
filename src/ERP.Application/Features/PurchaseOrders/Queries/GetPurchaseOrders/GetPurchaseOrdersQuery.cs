using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;

public sealed record GetPurchaseOrdersQuery : IRequest<IReadOnlyList<PurchaseOrderDto>>;
