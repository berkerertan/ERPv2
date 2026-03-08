using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.DeleteCariDebtItem;

public sealed class DeleteCariDebtItemCommandHandler(
    ICariDebtItemRepository debtItemRepository,
    ICariAccountRepository cariAccountRepository)
    : IRequestHandler<DeleteCariDebtItemCommand>
{
    public async Task Handle(DeleteCariDebtItemCommand request, CancellationToken cancellationToken)
    {
        var account = await cariAccountRepository.GetByIdAsync(request.CariAccountId, cancellationToken)
            ?? throw new NotFoundException("Cari account not found.");

        var item = await debtItemRepository.GetByIdAsync(request.CariDebtItemId, cancellationToken)
            ?? throw new NotFoundException("Cari debt item not found.");

        if (item.CariAccountId != request.CariAccountId)
        {
            throw new NotFoundException("Cari debt item not found for this cari account.");
        }

        await debtItemRepository.DeleteAsync(request.CariDebtItemId, cancellationToken);

        var remainingItems = await debtItemRepository.GetByCariAccountIdAsync(request.CariAccountId, cancellationToken);
        account.CurrentBalance = remainingItems.FirstOrDefault()?.RemainingBalance ?? 0m;
        await cariAccountRepository.UpdateAsync(account, cancellationToken);
    }
}
