using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IStockMovementRepository : IRepository<StockMovement>
{
    Task<decimal> GetCurrentQuantityAsync(Guid warehouseId, Guid productId, CancellationToken cancellationToken = default);
}
