using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IInventoryCountSessionRepository : IRepository<InventoryCountSession>
{
    Task<InventoryCountSession?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InventoryCountSession?> GetByClientRequestIdAsync(string clientRequestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryCountSession>> GetFilteredAsync(
        Guid? warehouseId,
        bool includeCompleted,
        CancellationToken cancellationToken = default);
    Task AddWithItemsAsync(
        InventoryCountSession session,
        IEnumerable<InventoryCountSessionItem> items,
        CancellationToken cancellationToken = default);
}
