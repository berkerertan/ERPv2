namespace ERP.API.Contracts.CariAccounts;

public sealed record CreateCariDebtItemRequest(
    DateTime TransactionDate,
    string MaterialDescription,
    decimal Quantity,
    decimal ListPrice,
    decimal SalePrice,
    decimal TotalAmount,
    decimal Payment,
    decimal RemainingBalance);
