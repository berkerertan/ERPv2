using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.ApplyInventoryCount;

public sealed class ApplyInventoryCountCommandHandler(
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository,
    IStockMovementRepository stockMovementRepository,
    IInventoryCountSessionRepository inventoryCountSessionRepository)
    : IRequestHandler<ApplyInventoryCountCommand, ApplyInventoryCountResult>
{
    public async Task<ApplyInventoryCountResult> Handle(ApplyInventoryCountCommand request, CancellationToken cancellationToken)
    {
        var clientRequestId = NormalizeText(request.ClientRequestId, 64);
        if (!string.IsNullOrWhiteSpace(clientRequestId))
        {
            var existingSession = await inventoryCountSessionRepository.GetByClientRequestIdAsync(clientRequestId, cancellationToken);
            if (existingSession is not null)
            {
                return new ApplyInventoryCountResult(
                    existingSession.Id,
                    existingSession.ReferenceNo,
                    existingSession.SubmittedItems,
                    existingSession.AppliedItems,
                    existingSession.SkippedItems,
                    existingSession.TotalIncreaseQuantity,
                    existingSession.TotalDecreaseQuantity);
            }
        }

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
        var session = await ResolveSessionAsync(request, cancellationToken);
        var referenceNo = session.ReferenceNo;
        var notes = NormalizeText(request.Notes, 500);
        var appliedMovements = new List<StockMovement>();
        var sessionItems = new List<InventoryCountSessionItem>();
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
                InventoryCountSessionId = session.Id,
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

            sessionItems.Add(new InventoryCountSessionItem
            {
                InventoryCountSessionId = session.Id,
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                Barcode = product.BarcodeEan13,
                Unit = product.Unit,
                LocationCode = NormalizeText(request.LocationCode, 100) ?? session.LocationCode,
                CountedByUserId = request.StartedByUserId,
                CountedByUserName = NormalizeText(request.StartedByUserName, 100),
                SystemQuantity = systemQuantity,
                CountedQuantity = item.CountedQuantity,
                DifferenceQuantity = difference,
                CountedAtUtc = DateTime.UtcNow
            });
        }

        if (appliedMovements.Count > 0)
        {
            await stockMovementRepository.AddRangeAsync(appliedMovements, cancellationToken);
        }

        session.ReferenceNo = referenceNo;
        session.ClientRequestId = clientRequestId ?? session.ClientRequestId;
        session.Notes = notes ?? session.Notes;
        session.LocationCode = NormalizeText(request.LocationCode, 100) ?? session.LocationCode;
        session.StartedByUserId ??= request.StartedByUserId;
        session.StartedByUserName ??= NormalizeText(request.StartedByUserName, 100);
        session.SubmittedItems = submittedItems;
        session.AppliedItems = appliedMovements.Count;
        session.SkippedItems = submittedItems - appliedMovements.Count;
        session.TotalIncreaseQuantity = totalIncreaseQuantity;
        session.TotalDecreaseQuantity = totalDecreaseQuantity;
        session.CompletedAtUtc = DateTime.UtcNow;
        session.Status = InventoryCountSessionStatus.Applied;

        if (request.SessionId.HasValue)
        {
            session.Items = sessionItems;
            await inventoryCountSessionRepository.UpdateAsync(session, cancellationToken);
        }
        else
        {
            await inventoryCountSessionRepository.AddWithItemsAsync(session, sessionItems, cancellationToken);
        }

        return new ApplyInventoryCountResult(
            session.Id,
            referenceNo,
            submittedItems,
            appliedMovements.Count,
            submittedItems - appliedMovements.Count,
            totalIncreaseQuantity,
            totalDecreaseQuantity);
    }

    private async Task<InventoryCountSession> ResolveSessionAsync(
        ApplyInventoryCountCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SessionId.HasValue)
        {
            var existing = await inventoryCountSessionRepository.GetWithItemsAsync(request.SessionId.Value, cancellationToken);
            if (existing is null)
            {
                throw new NotFoundException("Inventory count session not found.");
            }

            if (existing.Status != InventoryCountSessionStatus.Open)
            {
                throw new ConflictException("Inventory count session is already closed.");
            }

            if (existing.WarehouseId != request.WarehouseId)
            {
                throw new ConflictException("Inventory count session warehouse mismatch.");
            }

            return existing;
        }

        return new InventoryCountSession
        {
            WarehouseId = request.WarehouseId,
            Status = InventoryCountSessionStatus.Applied,
            ReferenceNo = NormalizeReferenceNo(request.ReferenceNo),
            ClientRequestId = NormalizeText(request.ClientRequestId, 64),
            Notes = NormalizeText(request.Notes, 500),
            LocationCode = NormalizeText(request.LocationCode, 100),
            StartedByUserId = request.StartedByUserId,
            StartedByUserName = NormalizeText(request.StartedByUserName, 100),
            StartedAtUtc = DateTime.UtcNow
        };
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
