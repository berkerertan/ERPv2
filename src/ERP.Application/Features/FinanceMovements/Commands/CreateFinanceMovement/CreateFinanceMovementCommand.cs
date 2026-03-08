using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.FinanceMovements.Commands.CreateFinanceMovement;

public sealed record CreateFinanceMovementCommand(
    Guid CariAccountId,
    FinanceMovementType Type,
    decimal Amount,
    string? Description,
    string? ReferenceNo) : IRequest<Guid>;
