using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.Branches.Queries.GetBranches;
using MediatR;

namespace ERP.Application.Features.Branches.Queries.GetBranchById;

public sealed class GetBranchByIdQueryHandler(IBranchRepository branchRepository)
    : IRequestHandler<GetBranchByIdQuery, BranchDto>
{
    public async Task<BranchDto> Handle(GetBranchByIdQuery request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new NotFoundException("Branch not found.");

        return new BranchDto(branch.Id, branch.CompanyId, branch.Code, branch.Name);
    }
}
