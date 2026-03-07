using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class CompanyRepository : InMemoryRepository<Company>, ICompanyRepository
{
    private readonly InMemoryDataStore _store;

    public CompanyRepository(InMemoryDataStore store) : base(store)
    {
        _store = store;
    }

    protected override List<Company> Entities => _store.Companies;

    public Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(Entities.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
