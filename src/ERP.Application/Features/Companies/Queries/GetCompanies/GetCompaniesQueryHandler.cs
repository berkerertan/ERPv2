using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.Companies.Queries.GetCompanies;

public sealed class GetCompaniesQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    public async Task<IReadOnlyList<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var companies = await companyRepository.GetAllAsync(cancellationToken);

        return companies
            .OrderBy(x => x.Code)
            .Select(x => new CompanyDto(x.Id, x.Code, x.Name, x.TaxNumber))
            .ToList();
    }
}
