using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.Reports.Queries.GetIncomeExpenseSummary;

public sealed class GetIncomeExpenseSummaryQueryHandler(
    ISalesOrderRepository salesOrderRepository,
    IPurchaseOrderRepository purchaseOrderRepository,
    IFinanceMovementRepository financeMovementRepository)
    : IRequestHandler<GetIncomeExpenseSummaryQuery, IncomeExpenseSummaryDto>
{
    public async Task<IncomeExpenseSummaryDto> Handle(GetIncomeExpenseSummaryQuery request, CancellationToken cancellationToken)
    {
        var sales = await salesOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var purchases = await purchaseOrderRepository.GetAllWithItemsAsync(cancellationToken);
        var financeMovements = await financeMovementRepository.GetAllAsync(cancellationToken);

        var income = sales
            .Where(x => x.Status == OrderStatus.Approved)
            .Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice));

        var expense = purchases
            .Where(x => x.Status == OrderStatus.Approved)
            .Sum(x => x.Items.Sum(i => i.Quantity * i.UnitPrice));

        var collections = financeMovements
            .Where(x => x.Type == FinanceMovementType.Collection)
            .Sum(x => x.Amount);

        var payments = financeMovements
            .Where(x => x.Type == FinanceMovementType.Payment)
            .Sum(x => x.Amount);

        return new IncomeExpenseSummaryDto(income, expense, income - expense, collections, payments);
    }
}
