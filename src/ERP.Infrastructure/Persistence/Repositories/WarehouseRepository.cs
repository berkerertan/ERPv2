using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Infrastructure.Persistence.Repositories;

public sealed class WarehouseRepository : EfRepository<Warehouse>, IWarehouseRepository
{
    private readonly ErpDbContext _dbContext;

    public WarehouseRepository(ErpDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Warehouse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Warehouses.FirstOrDefaultAsync(
            x => x.Code.ToLower() == code.ToLower(),
            cancellationToken);
    }
}
