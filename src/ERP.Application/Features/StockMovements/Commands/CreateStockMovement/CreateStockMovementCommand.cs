using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.CreateStockMovement;

public sealed record CreateStockMovementCommand(
    Guid WarehouseId,
    Guid ProductId,
    StockMovementType Type,
    decimal Quantity,
    decimal UnitPrice,
    string? ReferenceNo) : IRequest<Guid>;
