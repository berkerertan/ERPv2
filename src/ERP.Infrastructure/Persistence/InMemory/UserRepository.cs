using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;

namespace ERP.Infrastructure.Persistence.InMemory;

public sealed class UserRepository : InMemoryRepository<AppUser>, IUserRepository
{
    private readonly InMemoryDataStore _store;

    public UserRepository(InMemoryDataStore store) : base(store)
    {
        _store = store;
    }

    protected override List<AppUser> Entities => _store.Users;

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(Entities.Any(x => !x.IsDeleted));
        }
    }

    public Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                Entities.FirstOrDefault(x => !x.IsDeleted && x.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(
                Entities.FirstOrDefault(x => !x.IsDeleted && x.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
