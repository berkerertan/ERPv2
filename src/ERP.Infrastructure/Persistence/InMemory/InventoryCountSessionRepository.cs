using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class InventoryCountSessionRepository : InMemoryRepository<InventoryCountSession>, IInventoryCountSessionRepository
{
    private readonly InMemoryDataStore _store;

    public InventoryCountSessionRepository(InMemoryDataStore store) : base(store)
    {
        _store = store;
    }

    protected override List<InventoryCountSession> Entities => _store.InventoryCountSessions;

    public Task<InventoryCountSession?> GetWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var session = Entities.FirstOrDefault(x => x.Id == id && !x.IsDeleted);
            if (session is null)
            {
                return Task.FromResult<InventoryCountSession?>(null);
            }

            session.Items = _store.InventoryCountSessionItems
                .Where(x => !x.IsDeleted && x.InventoryCountSessionId == id)
                .OrderByDescending(x => x.CountedAtUtc)
                .ToList();

            return Task.FromResult<InventoryCountSession?>(session);
        }
    }

    public Task<IReadOnlyList<InventoryCountSession>> GetFilteredAsync(
        Guid? warehouseId,
        bool includeCompleted,
        CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            IEnumerable<InventoryCountSession> query = Entities.Where(x => !x.IsDeleted);
            if (warehouseId.HasValue)
            {
                query = query.Where(x => x.WarehouseId == warehouseId.Value);
            }

            if (!includeCompleted)
            {
                query = query.Where(x => x.Status == ERP.Domain.Enums.InventoryCountSessionStatus.Open);
            }

            return Task.FromResult<IReadOnlyList<InventoryCountSession>>(query.ToList());
        }
    }

    public Task AddWithItemsAsync(
        InventoryCountSession session,
        IEnumerable<InventoryCountSessionItem> items,
        CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            session.CreatedAtUtc = DateTime.UtcNow;
            Entities.Add(session);

            foreach (var item in items)
            {
                item.CreatedAtUtc = DateTime.UtcNow;
                _store.InventoryCountSessionItems.Add(item);
            }
        }

        return Task.CompletedTask;
    }

    public override Task UpdateAsync(InventoryCountSession entity, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var index = Entities.FindIndex(x => x.Id == entity.Id && !x.IsDeleted);
            if (index >= 0)
            {
                entity.UpdatedAtUtc = DateTime.UtcNow;
                Entities[index] = entity;

                _store.InventoryCountSessionItems.RemoveAll(x => !x.IsDeleted && x.InventoryCountSessionId == entity.Id);
                foreach (var item in entity.Items)
                {
                    item.CreatedAtUtc = DateTime.UtcNow;
                    _store.InventoryCountSessionItems.Add(item);
                }
            }
        }

        return Task.CompletedTask;
    }
}
