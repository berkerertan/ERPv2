using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.DeletePurchaseOrder;

public sealed record DeletePurchaseOrderCommand(Guid PurchaseOrderId) : IRequest;
