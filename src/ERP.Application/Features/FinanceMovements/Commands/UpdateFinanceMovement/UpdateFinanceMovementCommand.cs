using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.FinanceMovements.Commands.UpdateFinanceMovement;

public sealed record UpdateFinanceMovementCommand(
    Guid FinanceMovementId,
    Guid CariAccountId,
    FinanceMovementType Type,
    decimal Amount,
    string? Description,
    string? ReferenceNo) : IRequest;
