using Microsoft.AspNetCore.Http;

namespace ERP.API.Contracts.CariAccounts;

public sealed class ImportBuyerDebtItemsBatchRequest
{
    public List<IFormFile> Files { get; set; } = [];
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

public sealed record BuyerDebtItemsBatchImportFileResult(
    string FileName,
    Guid? CariAccountId,
    string CariAccountName,
    bool CariCreated,
    int TotalRows,
    int CreatedCount,
    int FailedCount,
    IReadOnlyList<string> Errors);

public sealed record BuyerDebtItemsBatchImportResult(
    int TotalFiles,
    int ProcessedFiles,
    int CreatedCariCount,
    int TotalRows,
    int TotalCreatedCount,
    int TotalFailedCount,
    IReadOnlyList<BuyerDebtItemsBatchImportFileResult> Files);
