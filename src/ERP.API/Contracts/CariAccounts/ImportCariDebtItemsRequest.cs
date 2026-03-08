using Microsoft.AspNetCore.Http;

namespace ERP.API.Contracts.CariAccounts;

public sealed class ImportCariDebtItemsRequest
{
    public IFormFile File { get; set; } = default!;
    public bool ReplaceExisting { get; set; } = false;

    public string? TransactionDateColumn { get; set; }
    public string? MaterialDescriptionColumn { get; set; }
    public string? QuantityColumn { get; set; }
    public string? ListPriceColumn { get; set; }
    public string? SalePriceColumn { get; set; }
    public string? TotalAmountColumn { get; set; }
    public string? PaymentColumn { get; set; }
    public string? RemainingBalanceColumn { get; set; }
}
