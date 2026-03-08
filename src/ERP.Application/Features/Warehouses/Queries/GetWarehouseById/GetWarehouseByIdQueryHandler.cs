using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.Warehouses.Queries.GetWarehouses;
using MediatR;

namespace ERP.Application.Features.Warehouses.Queries.GetWarehouseById;

public sealed class GetWarehouseByIdQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    public async Task<WarehouseDto> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new NotFoundException("Warehouse not found.");

        return new WarehouseDto(warehouse.Id, warehouse.BranchId, warehouse.Code, warehouse.Name);
    }
}
