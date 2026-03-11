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
            .OrderBy(x => x.Name)
            .Select(x =>
            {
                var total = x.CurrentBalance;
                var current = 0m;
                var days30 = 0m;
                var days60 = 0m;
                var days90 = 0m;
                var over90 = 0m;

                if (x.MaturityDays <= 0)
                {
                    current = total;
                }
                else if (x.MaturityDays <= 30)
                {
                    days30 = total;
                }
                else if (x.MaturityDays <= 60)
                {
                    days60 = total;
                }
                else if (x.MaturityDays <= 90)
                {
                    days90 = total;
                }
                else
                {
                    over90 = total;
                }

                return new CariAgingDto(x.Id, x.Name, current, days30, days60, days90, over90, total);
            })
            .ToList();
    }
}
