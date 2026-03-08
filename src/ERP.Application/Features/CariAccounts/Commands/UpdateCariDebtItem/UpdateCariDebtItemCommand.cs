using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.UpdateCariDebtItem;

public sealed record UpdateCariDebtItemCommand(
    Guid CariAccountId,
    Guid CariDebtItemId,
    DateTime TransactionDate,
    string MaterialDescription,
    decimal Quantity,
    decimal ListPrice,
    decimal SalePrice,
    decimal TotalAmount,
    decimal Payment,
    decimal RemainingBalance) : IRequest;
