using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.Warehouses.Queries.GetWarehouses;

public sealed class GetWarehousesQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    public async Task<IReadOnlyList<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await warehouseRepository.GetAllAsync(cancellationToken);

        return warehouses
            .OrderBy(x => x.Code)
            .Select(x => new WarehouseDto(x.Id, x.BranchId, x.Code, x.Name))
            .ToList();
    }
}
