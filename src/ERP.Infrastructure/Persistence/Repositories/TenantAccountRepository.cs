using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class TenantAccountRepository : EfRepository<TenantAccount>, ITenantAccountRepository
{
    private readonly ErpDbContext _dbContext;

    public TenantAccountRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantAccounts.FirstOrDefaultAsync(
            x => x.Code.ToLower() == code.ToLower(),
            cancellationToken);
    }
}
