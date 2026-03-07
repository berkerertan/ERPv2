using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<CreateCompanyCommand, Guid>
{
    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await companyRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            throw new ConflictException("Company code already exists.");
        }

        var company = new Company
        {
            Code = request.Code,
            Name = request.Name,
            TaxNumber = request.TaxNumber
        };

        await companyRepository.AddAsync(company, cancellationToken);
        return company.Id;
    }
}
