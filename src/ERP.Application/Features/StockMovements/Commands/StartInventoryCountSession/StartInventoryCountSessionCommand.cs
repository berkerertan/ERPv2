using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.StartInventoryCountSession;

public sealed record StartInventoryCountSessionCommand(
    Guid WarehouseId,
    string? ReferenceNo,
    string? Notes,
    string? LocationCode,
    Guid? StartedByUserId,
    string? StartedByUserName) : IRequest<Guid>;
