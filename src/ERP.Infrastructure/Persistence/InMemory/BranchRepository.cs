using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class BranchRepository : InMemoryRepository<Branch>, IBranchRepository
{
    private readonly InMemoryDataStore _store;

    public BranchRepository(InMemoryDataStore store) : base(store)
    {
        _store = store;
    }

    protected override List<Branch> Entities => _store.Branches;

    public Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(Entities.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
