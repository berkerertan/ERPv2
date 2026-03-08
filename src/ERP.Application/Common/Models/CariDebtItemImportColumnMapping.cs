namespace ERP.Application.Common.Models;

public sealed record CariDebtItemImportColumnMapping(
    string? TransactionDateColumn,
    string? MaterialDescriptionColumn,
    string? QuantityColumn,
    string? ListPriceColumn,
    string? SalePriceColumn,
    string? TotalAmountColumn,
    string? PaymentColumn,
    string? RemainingBalanceColumn);
