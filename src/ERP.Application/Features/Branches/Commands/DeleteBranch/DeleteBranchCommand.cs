using MediatR;

namespace ERP.Application.Features.Branches.Commands.DeleteBranch;

public sealed record DeleteBranchCommand(Guid BranchId) : IRequest;
