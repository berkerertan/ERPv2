using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.ApplyInventoryCount;

public sealed class ApplyInventoryCountCommandHandler(
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository)
    : IRequestHandler<ApplyInventoryCountCommand, ApplyInventoryCountResult>
{
    public async Task<ApplyInventoryCountResult> Handle(ApplyInventoryCountCommand request, CancellationToken cancellationToken)
    {
        if (await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Warehouse not found.");
        }

        var normalizedItems = request.Items
            .Where(x => x.ProductId != Guid.Empty)
            .GroupBy(x => x.ProductId)
            .Select(group => new ApplyInventoryCountItem(
                group.Key,
                group.Sum(x => x.CountedQuantity)))
            .ToList();

        if (normalizedItems.Count == 0)
        {
            throw new ConflictException("No inventory count items were supplied.");
        }

        var submittedItems = normalizedItems.Count;
        var referenceNo = NormalizeReferenceNo(request.ReferenceNo);
        var notes = NormalizeText(request.Notes, 500);
        var appliedMovements = new List<StockMovement>();
        decimal totalIncreaseQuantity = 0m;
        decimal totalDecreaseQuantity = 0m;

        foreach (var item in normalizedItems)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null)
            {
                throw new NotFoundException($"Product not found: {item.ProductId}");
            }

            var systemQuantity = await stockMovementRepository.GetCurrentQuantityAsync(
                request.WarehouseId,
                item.ProductId,
                cancellationToken);

            var difference = item.CountedQuantity - systemQuantity;
            if (difference == 0)
            {
                continue;
            }

            if (difference > 0)
            {
                totalIncreaseQuantity += difference;
            }
            else
            {
                totalDecreaseQuantity += Math.Abs(difference);
            }

            appliedMovements.Add(new StockMovement
            {
                WarehouseId = request.WarehouseId,
                ProductId = item.ProductId,
                Type = difference > 0 ? StockMovementType.In : StockMovementType.Out,
                Reason = StockMovementReason.InventoryAdjustment,
                Quantity = Math.Abs(difference),
                UnitPrice = 0m,
                ReferenceNo = referenceNo,
                ReasonNote = BuildReasonNote(notes, systemQuantity, item.CountedQuantity),
                MovementDateUtc = DateTime.UtcNow
            });
        }

        if (appliedMovements.Count > 0)
        {
            await stockMovementRepository.AddRangeAsync(appliedMovements, cancellationToken);
        }

        return new ApplyInventoryCountResult(
            referenceNo,
            submittedItems,
            appliedMovements.Count,
            submittedItems - appliedMovements.Count,
            totalIncreaseQuantity,
            totalDecreaseQuantity);
    }

    private static string NormalizeReferenceNo(string? value)
    {
        var normalized = NormalizeText(value, 200);
        return string.IsNullOrWhiteSpace(normalized)
            ? $"Sayim duzeltmesi - {DateTime.UtcNow:yyyy-MM-dd HH:mm}"
            : normalized;
    }

    private static string? BuildReasonNote(string? notes, decimal systemQuantity, decimal countedQuantity)
    {
        var summary = $"System: {systemQuantity:0.###}, Counted: {countedQuantity:0.###}";
        return string.IsNullOrWhiteSpace(notes)
            ? summary
            : NormalizeText($"{notes} | {summary}", 500);
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
