using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetCariBalances;

public sealed class GetCariBalancesQueryHandler(
    ICariAccountRepository cariAccountRepository,
    IFinanceMovementRepository financeMovementRepository)
    : IRequestHandler<GetCariBalancesQuery, IReadOnlyList<CariBalanceDto>>
{
    public async Task<IReadOnlyList<CariBalanceDto>> Handle(GetCariBalancesQuery request, CancellationToken cancellationToken)
    {
        var cariAccounts = await cariAccountRepository.GetAllAsync(cancellationToken);
        var movements = await financeMovementRepository.GetAllAsync(cancellationToken);

        var lastMovementByCari = movements
            .GroupBy(x => x.CariAccountId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.MovementDateUtc));

        return cariAccounts
            .OrderBy(x => x.Name)
            .Select(x => new CariBalanceDto(
                x.Id,
                x.Name,
                x.Type,
                x.CurrentBalance,
                lastMovementByCari.TryGetValue(x.Id, out var lastDate) ? lastDate : null))
            .ToList();
    }
}
