using MediatR;

namespace ERP.Application.Features.PurchaseOrders.Commands.ApprovePurchaseOrder;

public sealed record ApprovePurchaseOrderCommand(
    Guid PurchaseOrderId,
    Guid? ApprovedByUserId,
    string? ApprovedByUserName) : IRequest;
