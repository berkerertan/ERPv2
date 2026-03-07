using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.Branches.Queries.GetBranches;

public sealed class GetBranchesQueryHandler(IBranchRepository branchRepository)
    : IRequestHandler<GetBranchesQuery, IReadOnlyList<BranchDto>>
{
    public async Task<IReadOnlyList<BranchDto>> Handle(GetBranchesQuery request, CancellationToken cancellationToken)
    {
        var branches = await branchRepository.GetAllAsync(cancellationToken);

        return branches
            .OrderBy(x => x.Code)
            .Select(x => new BranchDto(x.Id, x.CompanyId, x.Code, x.Name))
            .ToList();
    }
}
