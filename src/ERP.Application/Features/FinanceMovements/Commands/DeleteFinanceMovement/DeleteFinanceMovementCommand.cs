using MediatR;

namespace ERP.Application.Features.FinanceMovements.Commands.DeleteFinanceMovement;

public sealed record DeleteFinanceMovementCommand(Guid FinanceMovementId) : IRequest;
