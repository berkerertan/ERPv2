using ERP.Application.Features.Branches.Queries.GetBranches;
using MediatR;

namespace ERP.Application.Features.Branches.Queries.GetBranchById;

public sealed record GetBranchByIdQuery(Guid BranchId) : IRequest<BranchDto>;
