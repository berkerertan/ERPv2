using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetIncomeExpenseSummary;

public sealed record GetIncomeExpenseSummaryQuery : IRequest<IncomeExpenseSummaryDto>;
