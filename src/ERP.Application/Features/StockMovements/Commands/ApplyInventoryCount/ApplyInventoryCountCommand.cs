using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.ApplyInventoryCount;

public sealed record ApplyInventoryCountCommand(
    Guid WarehouseId,
    string? ReferenceNo,
    string? Notes,
    IReadOnlyList<ApplyInventoryCountItem> Items) : IRequest<ApplyInventoryCountResult>;

public sealed record ApplyInventoryCountItem(
    Guid ProductId,
    decimal CountedQuantity);
