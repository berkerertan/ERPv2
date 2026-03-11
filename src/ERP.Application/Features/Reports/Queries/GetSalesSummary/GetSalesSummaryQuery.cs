using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetSalesSummary;

public sealed record GetSalesSummaryQuery : IRequest<IReadOnlyList<SalesReportItemDto>>;
