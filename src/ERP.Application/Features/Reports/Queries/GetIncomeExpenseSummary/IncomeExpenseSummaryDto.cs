namespace ERP.Application.Features.Reports.Queries.GetIncomeExpenseSummary;

public sealed record IncomeExpenseSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetProfit,
    decimal TotalCollections,
    decimal TotalPayments);
