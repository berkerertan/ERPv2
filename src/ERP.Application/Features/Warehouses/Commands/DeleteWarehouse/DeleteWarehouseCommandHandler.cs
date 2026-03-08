using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Warehouses.Commands.DeleteWarehouse;

public sealed class DeleteWarehouseCommandHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<DeleteWarehouseCommand>
{
    public async Task Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        if (await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Warehouse not found.");
        }

        await warehouseRepository.DeleteAsync(request.WarehouseId, cancellationToken);
    }
}
