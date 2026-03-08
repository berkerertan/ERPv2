using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IUserRepository : IRepository<AppUser>
{
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
