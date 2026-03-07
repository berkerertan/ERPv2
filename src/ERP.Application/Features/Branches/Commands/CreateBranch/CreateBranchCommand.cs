using MediatR;

namespace ERP.Application.Features.Branches.Commands.CreateBranch;

public sealed record CreateBranchCommand(Guid CompanyId, string Code, string Name) : IRequest<Guid>;
