using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.StockMovements.Commands.StartInventoryCountSession;

public sealed class StartInventoryCountSessionCommandHandler(
    IWarehouseRepository warehouseRepository,
    IInventoryCountSessionRepository inventoryCountSessionRepository)
    : IRequestHandler<StartInventoryCountSessionCommand, Guid>
{
    public async Task<Guid> Handle(StartInventoryCountSessionCommand request, CancellationToken cancellationToken)
    {
        if (await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Warehouse not found.");
        }

        var session = new InventoryCountSession
        {
            WarehouseId = request.WarehouseId,
            Status = InventoryCountSessionStatus.Open,
            ReferenceNo = NormalizeReferenceNo(request.ReferenceNo),
            Notes = NormalizeText(request.Notes, 500),
            LocationCode = NormalizeText(request.LocationCode, 100),
            StartedByUserId = request.StartedByUserId,
            StartedByUserName = NormalizeText(request.StartedByUserName, 100),
            StartedAtUtc = DateTime.UtcNow
        };

        await inventoryCountSessionRepository.AddAsync(session, cancellationToken);
        return session.Id;
    }

    private static string NormalizeReferenceNo(string? value)
    {
        var normalized = NormalizeText(value, 200);
        return string.IsNullOrWhiteSpace(normalized)
            ? $"Sayim oturumu - {DateTime.UtcNow:yyyy-MM-dd HH:mm}"
            : normalized;
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
