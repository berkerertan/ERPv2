using ERP.Domain.Enums;

namespace ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovements;

public sealed record FinanceMovementDto(
    Guid Id,
    Guid CariAccountId,
    FinanceMovementType Type,
    decimal Amount,
    DateTime MovementDateUtc,
    string? Description,
    string? ReferenceNo);
