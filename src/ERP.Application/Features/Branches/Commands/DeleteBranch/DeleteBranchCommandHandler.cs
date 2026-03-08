using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Branches.Commands.DeleteBranch;

public sealed class DeleteBranchCommandHandler(IBranchRepository branchRepository)
    : IRequestHandler<DeleteBranchCommand>
{
    public async Task Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        if (await branchRepository.GetByIdAsync(request.BranchId, cancellationToken) is null)
        {
            throw new NotFoundException("Branch not found.");
        }

        await branchRepository.DeleteAsync(request.BranchId, cancellationToken);
    }
}
