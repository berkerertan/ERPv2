using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.DeleteStockMovement;

public sealed record DeleteStockMovementCommand(Guid StockMovementId) : IRequest;
