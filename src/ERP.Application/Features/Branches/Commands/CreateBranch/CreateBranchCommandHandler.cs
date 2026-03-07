using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Branches.Commands.CreateBranch;

public sealed class CreateBranchCommandHandler(ICompanyRepository companyRepository, IBranchRepository branchRepository)
    : IRequestHandler<CreateBranchCommand, Guid>
{
    public async Task<Guid> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        if (await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken) is null)
        {
            throw new NotFoundException("Company not found.");
        }

        if (await branchRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            throw new ConflictException("Branch code already exists.");
        }

        var branch = new Branch
        {
            CompanyId = request.CompanyId,
            Code = request.Code,
            Name = request.Name
        };

        await branchRepository.AddAsync(branch, cancellationToken);
        return branch.Id;
    }
}
