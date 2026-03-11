using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetSalesSummary;

public sealed class GetSalesSummaryQueryHandler(
    ISalesOrderRepository salesOrderRepository,
    IProductRepository productRepository)
    : IRequestHandler<GetSalesSummaryQuery, IReadOnlyList<SalesReportItemDto>>
{
    public async Task<IReadOnlyList<SalesReportItemDto>> Handle(GetSalesSummaryQuery request, CancellationToken cancellationToken)
    {
        var orders = await salesOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var approved = orders.Where(x => x.Status == OrderStatus.Approved).ToList();
        var products = (await productRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.Name);

        return approved
            .GroupBy(x => DateOnly.FromDateTime(x.OrderDateUtc.Date))
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var topProductId = group
                    .SelectMany(x => x.Items)
                    .GroupBy(i => i.ProductId)
                    .OrderByDescending(g => g.Sum(i => i.Quantity))
                    .Select(g => g.Key)
                    .FirstOrDefault();

                var topProduct = topProductId != Guid.Empty && products.TryGetValue(topProductId, out var name)
                    ? name
                    : string.Empty;

                return new SalesReportItemDto(
                    group.Key,
                    group.Count(),
                    group.Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice)),
                    topProduct);
            })
            .ToList();
    }
}
