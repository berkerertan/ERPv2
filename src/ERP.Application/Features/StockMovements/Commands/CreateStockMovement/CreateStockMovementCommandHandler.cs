using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.CreateStockMovement;

public sealed class CreateStockMovementCommandHandler(
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository)
    : IRequestHandler<CreateStockMovementCommand, Guid>
{
    public async Task<Guid> Handle(CreateStockMovementCommand request, CancellationToken cancellationToken)
    {
        if (await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Warehouse not found.");
        }

        if (await productRepository.GetByIdAsync(request.ProductId, cancellationToken) is null)
        {
            throw new NotFoundException("Product not found.");
        }

        if (request.Type == StockMovementType.Out)
        {
            var current = await stockMovementRepository.GetCurrentQuantityAsync(
                request.WarehouseId,
                request.ProductId,
                cancellationToken);

            if (current < request.Quantity)
            {
                throw new ConflictException("Insufficient stock quantity.");
            }
        }

        var movement = new StockMovement
        {
            WarehouseId = request.WarehouseId,
            ProductId = request.ProductId,
            Type = request.Type,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            ReferenceNo = request.ReferenceNo,
            MovementDateUtc = DateTime.UtcNow
        };

        await stockMovementRepository.AddAsync(movement, cancellationToken);
        return movement.Id;
    }
}
