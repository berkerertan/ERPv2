namespace ERP.Application.Features.Reports.Queries.GetIncomeExpenseSummary;

public sealed record IncomeExpenseItemDto(DateOnly Date, decimal Income, decimal Expense);

public sealed record IncomeExpenseSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetProfit,
    IReadOnlyList<IncomeExpenseItemDto> Items);
