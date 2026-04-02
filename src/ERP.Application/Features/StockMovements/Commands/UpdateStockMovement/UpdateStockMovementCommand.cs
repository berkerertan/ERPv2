using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.UpdateStockMovement;

public sealed record UpdateStockMovementCommand(
    Guid StockMovementId,
    Guid WarehouseId,
    Guid ProductId,
    StockMovementType Type,
    decimal Quantity,
    decimal UnitPrice,
    string? ReferenceNo,
    StockMovementReason? Reason = null,
    string? ReasonNote = null,
    string? ProofImageUrl = null,
    string? ProofImagePublicId = null) : IRequest;
