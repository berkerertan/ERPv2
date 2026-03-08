namespace ERP.Application.Common.Models;

public sealed record CariDebtItemExcelRow(
    int RowNumber,
    string TransactionDate,
    string MaterialDescription,
    string Quantity,
    string ListPrice,
    string SalePrice,
    string TotalAmount,
    string Payment,
    string RemainingBalance);
