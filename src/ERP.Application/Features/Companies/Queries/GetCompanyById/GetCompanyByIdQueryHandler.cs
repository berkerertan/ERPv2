using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.Companies.Queries.GetCompanies;
using MediatR;

namespace ERP.Application.Features.Companies.Queries.GetCompanyById;

public sealed class GetCompanyByIdQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDto>
{
    public async Task<CompanyDto> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Company not found.");

        return new CompanyDto(company.Id, company.Code, company.Name, company.TaxNumber);
    }
}
