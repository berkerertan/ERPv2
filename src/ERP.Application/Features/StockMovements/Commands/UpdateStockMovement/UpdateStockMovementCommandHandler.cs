using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.UpdateStockMovement;

public sealed class UpdateStockMovementCommandHandler(
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository)
    : IRequestHandler<UpdateStockMovementCommand>
{
    public async Task Handle(UpdateStockMovementCommand request, CancellationToken cancellationToken)
    {
        var movement = await stockMovementRepository.GetByIdAsync(request.StockMovementId, cancellationToken)
            ?? throw new NotFoundException("Stock movement not found.");

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
            var current = await stockMovementRepository.GetCurrentQuantityAsync(request.WarehouseId, request.ProductId, cancellationToken);
            var currentMovementSigned = GetSignedQuantity(
                movement.WarehouseId == request.WarehouseId && movement.ProductId == request.ProductId
                    ? movement
                    : null);

            var availableBeforeUpdate = current - currentMovementSigned;

            if (availableBeforeUpdate < request.Quantity)
            {
                throw new ConflictException("Insufficient stock quantity.");
            }
        }

        movement.WarehouseId = request.WarehouseId;
        movement.ProductId = request.ProductId;
        movement.Type = request.Type;
        movement.Quantity = request.Quantity;
        movement.UnitPrice = request.UnitPrice;
        movement.ReferenceNo = request.ReferenceNo;

        await stockMovementRepository.UpdateAsync(movement, cancellationToken);
    }

    private static decimal GetSignedQuantity(Domain.Entities.StockMovement? movement)
    {
        if (movement is null)
        {
            return 0m;
        }

        return movement.Type == StockMovementType.In ? movement.Quantity : -movement.Quantity;
    }
}
