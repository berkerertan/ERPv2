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

        var reason = request.Reason ?? StockMovementReason.ManualAdjustment;
        if (!IsReasonCompatible(request.Type, reason))
        {
            throw new ConflictException("Selected movement reason is not compatible with movement type.");
        }

        var normalizedReasonNote = NormalizeText(request.ReasonNote, 500);
        var normalizedProofUrl = NormalizeText(request.ProofImageUrl, 1000);
        var normalizedProofPublicId = NormalizeText(request.ProofImagePublicId, 300);

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
            Reason = reason,
            ReasonNote = normalizedReasonNote,
            ProofImageUrl = normalizedProofUrl,
            ProofImagePublicId = normalizedProofPublicId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            ReferenceNo = request.ReferenceNo,
            MovementDateUtc = DateTime.UtcNow
        };

        await stockMovementRepository.AddAsync(movement, cancellationToken);
        return movement.Id;
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
