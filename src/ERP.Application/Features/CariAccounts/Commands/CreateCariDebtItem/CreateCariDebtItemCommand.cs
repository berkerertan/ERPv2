using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.CreateCariDebtItem;

public sealed record CreateCariDebtItemCommand(
    Guid CariAccountId,
    DateTime TransactionDate,
    string MaterialDescription,
    decimal Quantity,
    decimal ListPrice,
    decimal SalePrice,
    decimal TotalAmount,
    decimal Payment,
    decimal RemainingBalance) : IRequest<Guid>;
