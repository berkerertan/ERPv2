using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class CariAccountRepository : InMemoryRepository<CariAccount>, ICariAccountRepository
{
    private readonly InMemoryDataStore _store;

    public CariAccountRepository(InMemoryDataStore store) : base(store)
    {
        _store = store;
    }

    protected override List<CariAccount> Entities => _store.CariAccounts;

    public Task<CariAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(Entities.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
