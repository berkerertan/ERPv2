using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Warehouses.Commands.UpdateWarehouse;

public sealed class UpdateWarehouseCommandHandler(
    IWarehouseRepository warehouseRepository,
    IBranchRepository branchRepository)
    : IRequestHandler<UpdateWarehouseCommand>
{
    public async Task Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
            ?? throw new NotFoundException("Warehouse not found.");

        if (await branchRepository.GetByIdAsync(request.BranchId, cancellationToken) is null)
        {
            throw new NotFoundException("Branch not found.");
        }

        var codeOwner = await warehouseRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != warehouse.Id)
        {
            throw new ConflictException("Warehouse code already exists.");
        }

        warehouse.BranchId = request.BranchId;
        warehouse.Code = request.Code;
        warehouse.Name = request.Name;

        await warehouseRepository.UpdateAsync(warehouse, cancellationToken);
    }
}
