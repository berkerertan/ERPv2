using MediatR;

namespace ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovements;

public sealed record GetFinanceMovementsQuery : IRequest<IReadOnlyList<FinanceMovementDto>>;
