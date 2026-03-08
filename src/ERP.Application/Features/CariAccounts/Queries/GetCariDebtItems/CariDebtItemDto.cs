namespace ERP.Application.Features.CariAccounts.Queries.GetCariDebtItems;

public sealed record CariDebtItemDto(
    Guid Id,
    Guid CariAccountId,
    DateTime TransactionDate,
    string MaterialDescription,
    decimal Quantity,
    decimal ListPrice,
    decimal SalePrice,
    decimal TotalAmount,
    decimal Payment,
    decimal RemainingBalance);
