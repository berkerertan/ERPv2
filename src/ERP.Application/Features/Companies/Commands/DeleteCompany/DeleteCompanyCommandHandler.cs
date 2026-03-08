using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Companies.Commands.DeleteCompany;

public sealed class DeleteCompanyCommandHandler(ICompanyRepository companyRepository)
    : IRequestHandler<DeleteCompanyCommand>
{
    public async Task Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken) is null)
        {
            throw new NotFoundException("Company not found.");
        }

        await companyRepository.DeleteAsync(request.CompanyId, cancellationToken);
    }
}
