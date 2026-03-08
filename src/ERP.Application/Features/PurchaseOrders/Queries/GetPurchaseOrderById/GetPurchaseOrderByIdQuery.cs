using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;
using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById;

public sealed record GetPurchaseOrderByIdQuery(Guid PurchaseOrderId) : IRequest<PurchaseOrderDto>;
