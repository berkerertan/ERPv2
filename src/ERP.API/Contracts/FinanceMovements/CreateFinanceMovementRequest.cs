using ERP.Domain.Enums;

namespace ERP.API.Contracts.FinanceMovements;

public sealed record CreateFinanceMovementRequest(
    Guid CariAccountId,
    FinanceMovementType Type,
    decimal Amount,
    string? Description,
    string? ReferenceNo);
