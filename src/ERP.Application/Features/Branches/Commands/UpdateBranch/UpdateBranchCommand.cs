using MediatR;

namespace ERP.Application.Features.Branches.Commands.UpdateBranch;

public sealed record UpdateBranchCommand(Guid BranchId, Guid CompanyId, string Code, string Name) : IRequest;
