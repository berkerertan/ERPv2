using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.UpdateCariDebtItem;

public sealed class UpdateCariDebtItemCommandHandler(
    ICariAccountRepository cariAccountRepository,
    ICariDebtItemRepository debtItemRepository)
    : IRequestHandler<UpdateCariDebtItemCommand>
{
    public async Task Handle(UpdateCariDebtItemCommand request, CancellationToken cancellationToken)
    {
        var account = await cariAccountRepository.GetByIdAsync(request.CariAccountId, cancellationToken)
            ?? throw new NotFoundException("Cari account not found.");

        var item = await debtItemRepository.GetByIdAsync(request.CariDebtItemId, cancellationToken)
            ?? throw new NotFoundException("Cari debt item not found.");

        if (item.CariAccountId != request.CariAccountId)
        {
            throw new NotFoundException("Cari debt item not found for this cari account.");
        }

        item.TransactionDate = request.TransactionDate;
        item.MaterialDescription = request.MaterialDescription;
        item.Quantity = request.Quantity;
        item.ListPrice = request.ListPrice;
        item.SalePrice = request.SalePrice;
        item.TotalAmount = request.TotalAmount;
        item.Payment = request.Payment;
        item.RemainingBalance = request.RemainingBalance;

        await debtItemRepository.UpdateAsync(item, cancellationToken);
        account.CurrentBalance = request.RemainingBalance;
        await cariAccountRepository.UpdateAsync(account, cancellationToken);
    }
}
