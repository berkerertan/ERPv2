using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Persistence;

public interface ICompanyRepository : IRepository<Company>
{
    Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
