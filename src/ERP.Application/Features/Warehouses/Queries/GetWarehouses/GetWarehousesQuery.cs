using MediatR;

namespace ERP.Application.Features.Warehouses.Queries.GetWarehouses;

public sealed record GetWarehousesQuery : IRequest<IReadOnlyList<WarehouseDto>>;
