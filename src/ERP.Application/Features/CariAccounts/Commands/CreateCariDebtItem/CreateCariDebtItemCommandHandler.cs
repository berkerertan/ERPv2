using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.CreateCariDebtItem;

public sealed class CreateCariDebtItemCommandHandler(
    ICariAccountRepository cariAccountRepository,
    ICariDebtItemRepository debtItemRepository)
    : IRequestHandler<CreateCariDebtItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateCariDebtItemCommand request, CancellationToken cancellationToken)
    {
        var account = await cariAccountRepository.GetByIdAsync(request.CariAccountId, cancellationToken)
            ?? throw new NotFoundException("Cari account not found.");

        var item = new CariDebtItem
        {
            CariAccountId = request.CariAccountId,
            TransactionDate = request.TransactionDate,
            MaterialDescription = request.MaterialDescription,
            Quantity = request.Quantity,
            ListPrice = request.ListPrice,
            SalePrice = request.SalePrice,
            TotalAmount = request.TotalAmount,
            Payment = request.Payment,
            RemainingBalance = request.RemainingBalance
        };

        await debtItemRepository.AddAsync(item, cancellationToken);
        account.CurrentBalance = request.RemainingBalance;
        await cariAccountRepository.UpdateAsync(account, cancellationToken);

        return item.Id;
    }
}
