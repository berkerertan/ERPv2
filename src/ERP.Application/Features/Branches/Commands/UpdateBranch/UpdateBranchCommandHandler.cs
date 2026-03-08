using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Branches.Commands.UpdateBranch;

public sealed class UpdateBranchCommandHandler(
    IBranchRepository branchRepository,
    ICompanyRepository companyRepository)
    : IRequestHandler<UpdateBranchCommand>
{
    public async Task Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new NotFoundException("Branch not found.");

        if (await companyRepository.GetByIdAsync(request.CompanyId, cancellationToken) is null)
        {
            throw new NotFoundException("Company not found.");
        }

        var codeOwner = await branchRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != branch.Id)
        {
            throw new ConflictException("Branch code already exists.");
        }

        branch.CompanyId = request.CompanyId;
        branch.Code = request.Code;
        branch.Name = request.Name;

        await branchRepository.UpdateAsync(branch, cancellationToken);
    }
}
