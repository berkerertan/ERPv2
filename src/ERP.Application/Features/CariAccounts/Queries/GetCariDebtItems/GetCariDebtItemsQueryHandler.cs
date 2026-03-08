using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariDebtItems;

public sealed class GetCariDebtItemsQueryHandler(
    ICariAccountRepository cariAccountRepository,
    ICariDebtItemRepository debtItemRepository)
    : IRequestHandler<GetCariDebtItemsQuery, IReadOnlyList<CariDebtItemDto>>
{
    public async Task<IReadOnlyList<CariDebtItemDto>> Handle(GetCariDebtItemsQuery request, CancellationToken cancellationToken)
    {
        if (await cariAccountRepository.GetByIdAsync(request.CariAccountId, cancellationToken) is null)
        {
            throw new NotFoundException("Cari account not found.");
        }

        var items = await debtItemRepository.GetByCariAccountIdAsync(request.CariAccountId, cancellationToken);

        return items
            .Select(x => new CariDebtItemDto(
                x.Id,
                x.CariAccountId,
                x.TransactionDate,
                x.MaterialDescription,
                x.Quantity,
                x.ListPrice,
                x.SalePrice,
                x.TotalAmount,
                x.Payment,
                x.RemainingBalance))
            .ToList();
    }
}
