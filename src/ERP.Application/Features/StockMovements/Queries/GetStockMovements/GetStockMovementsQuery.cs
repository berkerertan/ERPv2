using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetStockMovements;

public sealed record GetStockMovementsQuery : IRequest<IReadOnlyList<StockMovementDto>>;
