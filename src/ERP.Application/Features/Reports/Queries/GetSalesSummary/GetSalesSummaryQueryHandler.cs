using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetSalesSummary;

public sealed class GetSalesSummaryQueryHandler(ISalesOrderRepository salesOrderRepository)
    : IRequestHandler<GetSalesSummaryQuery, SalesSummaryDto>
{
    public async Task<SalesSummaryDto> Handle(GetSalesSummaryQuery request, CancellationToken cancellationToken)
    {
        var orders = await salesOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var approved = orders.Where(x => x.Status == OrderStatus.Approved).ToList();

        return new SalesSummaryDto(
            approved.Count,
            approved.Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice)),
            approved.Sum(x => x.Items.Sum(i => i.Quantity)));
    }
}
