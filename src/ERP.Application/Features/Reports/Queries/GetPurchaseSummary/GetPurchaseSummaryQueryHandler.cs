using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetPurchaseSummary;

public sealed class GetPurchaseSummaryQueryHandler(
    IPurchaseOrderRepository purchaseOrderRepository,
    ICariAccountRepository cariAccountRepository)
    : IRequestHandler<GetPurchaseSummaryQuery, IReadOnlyList<PurchaseReportItemDto>>
{
    public async Task<IReadOnlyList<PurchaseReportItemDto>> Handle(GetPurchaseSummaryQuery request, CancellationToken cancellationToken)
    {
        var orders = await purchaseOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var approved = orders.Where(x => x.Status == OrderStatus.Approved).ToList();
        var suppliers = (await cariAccountRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.Name);

        return approved
            .GroupBy(x => DateOnly.FromDateTime(x.OrderDateUtc.Date))
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var topSupplierId = group
                    .GroupBy(x => x.SupplierCariAccountId)
                    .OrderByDescending(g => g.Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice)))
                    .Select(g => g.Key)
                    .FirstOrDefault();

                var topSupplier = topSupplierId != Guid.Empty && suppliers.TryGetValue(topSupplierId, out var supplierName)
                    ? supplierName
                    : string.Empty;

                return new PurchaseReportItemDto(
                    group.Key,
                    group.Count(),
                    group.Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice)),
                    topSupplier);
            })
            .ToList();
    }
}
