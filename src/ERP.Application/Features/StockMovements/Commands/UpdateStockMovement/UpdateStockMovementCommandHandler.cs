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

        var reason = request.Reason ?? movement.Reason;
        if (!IsReasonCompatible(request.Type, reason))
        {
            throw new ConflictException("Selected movement reason is not compatible with movement type.");
        }

        var normalizedReasonNote = request.ReasonNote is null
            ? movement.ReasonNote
            : NormalizeText(request.ReasonNote, 500);

        var normalizedProofUrl = request.ProofImageUrl is null
            ? movement.ProofImageUrl
            : NormalizeText(request.ProofImageUrl, 1000);

        var normalizedProofPublicId = request.ProofImagePublicId is null
            ? movement.ProofImagePublicId
            : NormalizeText(request.ProofImagePublicId, 300);

        if (reason == StockMovementReason.WasteScrap)
        {
            if (string.IsNullOrWhiteSpace(normalizedReasonNote))
            {
                throw new ConflictException("Waste/Scrap movement requires a reason note.");
            }

            if (string.IsNullOrWhiteSpace(normalizedProofUrl))
            {
                throw new ConflictException("Waste/Scrap movement requires a proof document.");
            }
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
        movement.Reason = reason;
        movement.ReasonNote = normalizedReasonNote;
        movement.ProofImageUrl = normalizedProofUrl;
        movement.ProofImagePublicId = normalizedProofPublicId;
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

    private static bool IsReasonCompatible(StockMovementType type, StockMovementReason reason)
    {
        return reason switch
        {
            StockMovementReason.PurchaseApproval or
            StockMovementReason.TransferIn or
            StockMovementReason.ReturnIn => type == StockMovementType.In,

            StockMovementReason.SalesApproval or
            StockMovementReason.TransferOut or
            StockMovementReason.PosSale or
            StockMovementReason.WasteScrap or
            StockMovementReason.ReturnOut => type == StockMovementType.Out,

            StockMovementReason.ManualAdjustment or
            StockMovementReason.InventoryAdjustment => true,

            _ => false
        };
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
    }
}
