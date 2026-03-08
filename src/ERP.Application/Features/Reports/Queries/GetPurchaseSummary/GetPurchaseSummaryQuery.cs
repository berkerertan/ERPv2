using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetPurchaseSummary;

public sealed record GetPurchaseSummaryQuery : IRequest<PurchaseSummaryDto>;
