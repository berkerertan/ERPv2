using MediatR;

namespace ERP.Application.Features.Branches.Queries.GetBranches;

public sealed record GetBranchesQuery : IRequest<IReadOnlyList<BranchDto>>;
