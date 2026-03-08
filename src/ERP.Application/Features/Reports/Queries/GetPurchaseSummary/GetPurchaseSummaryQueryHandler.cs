using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetPurchaseSummary;

public sealed class GetPurchaseSummaryQueryHandler(IPurchaseOrderRepository purchaseOrderRepository)
    : IRequestHandler<GetPurchaseSummaryQuery, PurchaseSummaryDto>
{
    public async Task<PurchaseSummaryDto> Handle(GetPurchaseSummaryQuery request, CancellationToken cancellationToken)
    {
        var orders = await purchaseOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var approved = orders.Where(x => x.Status == OrderStatus.Approved).ToList();

        return new PurchaseSummaryDto(
            approved.Count,
            approved.Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice)),
            approved.Sum(x => x.Items.Sum(i => i.Quantity)));
    }
}
