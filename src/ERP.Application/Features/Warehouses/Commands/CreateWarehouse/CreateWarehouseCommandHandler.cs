using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Warehouses.Commands.CreateWarehouse;

public sealed class CreateWarehouseCommandHandler(IBranchRepository branchRepository, IWarehouseRepository warehouseRepository)
    : IRequestHandler<CreateWarehouseCommand, Guid>
{
    public async Task<Guid> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        if (await branchRepository.GetByIdAsync(request.BranchId, cancellationToken) is null)
        {
            throw new NotFoundException("Branch not found.");
        }

        if (await warehouseRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            throw new ConflictException("Warehouse code already exists.");
        }

        var warehouse = new Warehouse
        {
            BranchId = request.BranchId,
            Code = request.Code,
            Name = request.Name
        };

        await warehouseRepository.AddAsync(warehouse, cancellationToken);
        return warehouse.Id;
    }
}
