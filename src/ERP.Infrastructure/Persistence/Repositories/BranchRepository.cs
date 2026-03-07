using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class BranchRepository : EfRepository<Branch>, IBranchRepository
{
    private readonly ErpDbContext _dbContext;

    public BranchRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Branches.FirstOrDefaultAsync(
            x => x.Code.ToLower() == code.ToLower(),
            cancellationToken);
    }
}
