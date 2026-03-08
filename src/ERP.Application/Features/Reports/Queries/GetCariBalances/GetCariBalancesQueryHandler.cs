using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetCariBalances;

public sealed class GetCariBalancesQueryHandler(ICariAccountRepository cariAccountRepository)
    : IRequestHandler<GetCariBalancesQuery, IReadOnlyList<CariBalanceDto>>
{
    public async Task<IReadOnlyList<CariBalanceDto>> Handle(GetCariBalancesQuery request, CancellationToken cancellationToken)
    {
        var cariAccounts = await cariAccountRepository.GetAllAsync(cancellationToken);

        return cariAccounts
            .OrderBy(x => x.Code)
            .Select(x => new CariBalanceDto(x.Id, x.Code, x.Name, x.Type, x.CurrentBalance, x.RiskLimit))
            .ToList();
    }
}
