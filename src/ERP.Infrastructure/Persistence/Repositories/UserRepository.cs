using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : EfRepository<AppUser>, IUserRepository
{
    private readonly ErpDbContext _dbContext;

    public UserRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(cancellationToken);
    }

    public async Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(
            x => x.UserName.ToLower() == userName.ToLower(),
            cancellationToken);
    }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(
            x => x.Email.ToLower() == email.ToLower(),
            cancellationToken);
    }
}
