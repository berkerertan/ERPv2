using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Companies.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<UpdateCompanyCommand>
{
    public async Task Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken)
            ?? throw new NotFoundException("Company not found.");

        var codeOwner = await companyRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != company.Id)
        {
            throw new ConflictException("Company code already exists.");
        }

        company.Code = request.Code;
        company.Name = request.Name;
        company.TaxNumber = request.TaxNumber;

        await companyRepository.UpdateAsync(company, cancellationToken);
    }
}
