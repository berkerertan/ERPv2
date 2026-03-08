using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.CariAccounts.Queries.GetCariDebtItems;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariDebtItemById;

public sealed class GetCariDebtItemByIdQueryHandler(ICariDebtItemRepository debtItemRepository)
    : IRequestHandler<GetCariDebtItemByIdQuery, CariDebtItemDto>
{
    public async Task<CariDebtItemDto> Handle(GetCariDebtItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await debtItemRepository.GetByIdAsync(request.CariDebtItemId, cancellationToken)
            ?? throw new NotFoundException("Cari debt item not found.");

        if (item.CariAccountId != request.CariAccountId)
        {
            throw new NotFoundException("Cari debt item not found for this cari account.");
        }

        return new CariDebtItemDto(
            item.Id,
            item.CariAccountId,
            item.TransactionDate,
            item.MaterialDescription,
            item.Quantity,
            item.ListPrice,
            item.SalePrice,
            item.TotalAmount,
            item.Payment,
            item.RemainingBalance);
    }
}
