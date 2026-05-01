using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.ApplyInventoryCount;

public sealed record ApplyInventoryCountCommand(
    Guid? SessionId,
    Guid WarehouseId,
    string? ReferenceNo,
    string? Notes,
    string? LocationCode,
    Guid? StartedByUserId,
    string? StartedByUserName,
    IReadOnlyList<ApplyInventoryCountItem> Items) : IRequest<ApplyInventoryCountResult>;

public sealed record ApplyInventoryCountItem(
    Guid ProductId,
    decimal CountedQuantity);
