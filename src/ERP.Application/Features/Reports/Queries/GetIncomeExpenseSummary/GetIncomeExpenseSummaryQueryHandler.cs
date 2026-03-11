using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetIncomeExpenseSummary;

public sealed class GetIncomeExpenseSummaryQueryHandler(
    ISalesOrderRepository salesOrderRepository,
    IPurchaseOrderRepository purchaseOrderRepository)
    : IRequestHandler<GetIncomeExpenseSummaryQuery, IncomeExpenseSummaryDto>
{
    public async Task<IncomeExpenseSummaryDto> Handle(GetIncomeExpenseSummaryQuery request, CancellationToken cancellationToken)
    {
        var sales = await salesOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var purchases = await purchaseOrderRepository.GetAllWithItemsAsync(cancellationToken);

        var approvedSales = sales.Where(x => x.Status == OrderStatus.Approved).ToList();
        var approvedPurchases = purchases.Where(x => x.Status == OrderStatus.Approved).ToList();

        var incomeByDate = approvedSales
            .GroupBy(x => DateOnly.FromDateTime(x.OrderDateUtc.Date))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice)));

        var expenseByDate = approvedPurchases
            .GroupBy(x => DateOnly.FromDateTime(x.OrderDateUtc.Date))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice)));

        var allDates = incomeByDate.Keys
            .Union(expenseByDate.Keys)
            .OrderBy(x => x)
            .ToList();

        var items = allDates
            .Select(date => new IncomeExpenseItemDto(
                date,
                incomeByDate.TryGetValue(date, out var income) ? income : 0m,
                expenseByDate.TryGetValue(date, out var expense) ? expense : 0m))
            .ToList();

        var totalIncome = items.Sum(x => x.Income);
        var totalExpense = items.Sum(x => x.Expense);

        return new IncomeExpenseSummaryDto(totalIncome, totalExpense, totalIncome - totalExpense, items);
    }
}
