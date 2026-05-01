using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetInventoryCountSessionById;

public sealed record GetInventoryCountSessionByIdQuery(Guid SessionId) : IRequest<InventoryCountSessionDetailDto>;
