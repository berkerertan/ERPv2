using ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovements;
using MediatR;

namespace ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovementById;

public sealed record GetFinanceMovementByIdQuery(Guid FinanceMovementId) : IRequest<FinanceMovementDto>;
