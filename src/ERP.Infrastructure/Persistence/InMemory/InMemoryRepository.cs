using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Common;

namespace ERP.Infrastructure.Persistence.InMemory;

public abstract class InMemoryRepository<T>(InMemoryDataStore store) : IRepository<T>
    where T : BaseEntity
{
    protected abstract List<T> Entities { get; }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            return Task.FromResult(Entities.FirstOrDefault(x => x.Id == id));
        }
    }

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            return Task.FromResult<IReadOnlyList<T>>(Entities.ToList());
        }
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            entity.CreatedAtUtc = DateTime.UtcNow;
            Entities.Add(entity);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var index = Entities.FindIndex(x => x.Id == entity.Id);
            if (index >= 0)
            {
                entity.UpdatedAtUtc = DateTime.UtcNow;
                Entities[index] = entity;
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (store.SyncRoot)
        {
            var existing = Entities.FirstOrDefault(x => x.Id == id);
            if (existing is not null)
            {
                Entities.Remove(existing);
            }
        }

        return Task.CompletedTask;
    }
}
