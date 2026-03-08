using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.DeleteCariAccount;

public sealed class DeleteCariAccountCommandHandler(ICariAccountRepository repository)
    : IRequestHandler<DeleteCariAccountCommand>
{
    public async Task Handle(DeleteCariAccountCommand request, CancellationToken cancellationToken)
    {
        if (await repository.GetByIdAsync(request.CariAccountId, cancellationToken) is null)
        {
            throw new NotFoundException("Cari account not found.");
        }

        await repository.DeleteAsync(request.CariAccountId, cancellationToken);
    }
}
