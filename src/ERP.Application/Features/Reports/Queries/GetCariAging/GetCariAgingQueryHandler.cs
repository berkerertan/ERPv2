using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetCariAging;

public sealed class GetCariAgingQueryHandler(ICariAccountRepository cariAccountRepository)
    : IRequestHandler<GetCariAgingQuery, IReadOnlyList<CariAgingDto>>
{
    public async Task<IReadOnlyList<CariAgingDto>> Handle(GetCariAgingQuery request, CancellationToken cancellationToken)
    {
        var cariAccounts = await cariAccountRepository.GetAllAsync(cancellationToken);

        return cariAccounts
            .OrderBy(x => x.Code)
            .Select(x => new CariAgingDto(
                x.Id,
                x.Code,
                x.Name,
                x.MaturityDays,
                x.CurrentBalance,
                GetBucket(x.MaturityDays)))
            .ToList();
    }

    private static string GetBucket(int maturityDays)
    {
        if (maturityDays <= 30) return "0-30";
        if (maturityDays <= 60) return "31-60";
        if (maturityDays <= 90) return "61-90";
        return "90+";
    }
}
