using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.StockMovements.Queries.GetInventoryCountSessionById;

public sealed class GetInventoryCountSessionByIdQueryHandler(
    IInventoryCountSessionRepository inventoryCountSessionRepository,
    IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetInventoryCountSessionByIdQuery, InventoryCountSessionDetailDto>
{
    public async Task<InventoryCountSessionDetailDto> Handle(GetInventoryCountSessionByIdQuery request, CancellationToken cancellationToken)
    {
        var session = await inventoryCountSessionRepository.GetWithItemsAsync(request.SessionId, cancellationToken);
        if (session is null)
        {
            throw new NotFoundException("Inventory count session not found.");
        }

        var warehouseName = (await warehouseRepository.GetByIdAsync(session.WarehouseId, cancellationToken))?.Name ?? string.Empty;

        return new InventoryCountSessionDetailDto(
            session.Id,
            session.WarehouseId,
            warehouseName,
            session.Status,
            session.ReferenceNo,
            session.Notes,
            session.LocationCode,
            session.StartedByUserId,
            session.StartedByUserName,
            session.StartedAtUtc,
            session.CompletedAtUtc,
            session.SubmittedItems,
            session.AppliedItems,
            session.SkippedItems,
            session.TotalIncreaseQuantity,
            session.TotalDecreaseQuantity,
            session.Items
                .OrderByDescending(x => x.CountedAtUtc)
                .Select(x => new InventoryCountSessionItemDto(
                    x.Id,
                    x.ProductId,
                    x.ProductCode,
                    x.ProductName,
                    x.Barcode,
                    x.Unit,
                    x.LocationCode,
                    x.CountedByUserId,
                    x.CountedByUserName,
                    x.SystemQuantity,
                    x.CountedQuantity,
                    x.DifferenceQuantity,
                    x.CountedAtUtc))
                .ToList());
    }
}
