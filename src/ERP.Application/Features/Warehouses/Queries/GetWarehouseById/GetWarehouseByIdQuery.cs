using ERP.Application.Features.Warehouses.Queries.GetWarehouses;
using MediatR;

namespace ERP.Application.Features.Warehouses.Queries.GetWarehouseById;

public sealed record GetWarehouseByIdQuery(Guid WarehouseId) : IRequest<WarehouseDto>;
