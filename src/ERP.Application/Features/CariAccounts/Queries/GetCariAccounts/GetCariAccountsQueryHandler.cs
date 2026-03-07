using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;

public sealed class GetCariAccountsQueryHandler(ICariAccountRepository repository)
    : IRequestHandler<GetCariAccountsQuery, IReadOnlyList<CariAccountDto>>
{
    public async Task<IReadOnlyList<CariAccountDto>> Handle(
        GetCariAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await repository.GetAllAsync(cancellationToken);

        return accounts
            .OrderBy(x => x.Code)
            .Select(x => new CariAccountDto(
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.RiskLimit,
                x.MaturityDays,
                x.CurrentBalance))
            .ToList();
    }
}
