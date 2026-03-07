using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class CariAccountRepository : EfRepository<CariAccount>, ICariAccountRepository
{
    private readonly ErpDbContext _dbContext;

    public CariAccountRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CariAccount?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CariAccounts.FirstOrDefaultAsync(
            x => x.Code.ToLower() == code.ToLower(),
            cancellationToken);
    }
}
