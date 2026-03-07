using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface IBranchRepository : IRepository<Branch>
{
    Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
