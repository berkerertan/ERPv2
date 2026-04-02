using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.TransferStock;

public sealed class TransferStockCommandHandler(
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository)
    : IRequestHandler<TransferStockCommand, TransferStockResult>
{
    public async Task<TransferStockResult> Handle(TransferStockCommand request, CancellationToken cancellationToken)
    {
        if (await warehouseRepository.GetByIdAsync(request.SourceWarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Source warehouse not found.");
        }

        if (await warehouseRepository.GetByIdAsync(request.DestinationWarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Destination warehouse not found.");
        }

        if (await productRepository.GetByIdAsync(request.ProductId, cancellationToken) is null)
        {
            throw new NotFoundException("Product not found.");
        }

        var available = await stockMovementRepository.GetCurrentQuantityAsync(
            request.SourceWarehouseId,
            request.ProductId,
            cancellationToken);

        if (available < request.Quantity)
        {
            throw new ConflictException($"Insufficient stock in source warehouse. Available: {available}, requested: {request.Quantity}.");
        }

        var referenceNo = string.IsNullOrWhiteSpace(request.ReferenceNo)
            ? $"TRF-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
            : request.ReferenceNo.Trim();

        var movementDateUtc = DateTime.UtcNow;

        var outMovement = new StockMovement
        {
            WarehouseId = request.SourceWarehouseId,
            ProductId = request.ProductId,
            Type = StockMovementType.Out,
            Reason = StockMovementReason.TransferOut,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            ReferenceNo = referenceNo,
            MovementDateUtc = movementDateUtc
        };

        await stockMovementRepository.AddAsync(outMovement, cancellationToken);

        var inMovement = new StockMovement
        {
            WarehouseId = request.DestinationWarehouseId,
            ProductId = request.ProductId,
            Type = StockMovementType.In,
            Reason = StockMovementReason.TransferIn,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            ReferenceNo = referenceNo,
            MovementDateUtc = movementDateUtc
        };

        await stockMovementRepository.AddAsync(inMovement, cancellationToken);

        return new TransferStockResult(outMovement.Id, inMovement.Id, referenceNo, movementDateUtc);
    }
}
