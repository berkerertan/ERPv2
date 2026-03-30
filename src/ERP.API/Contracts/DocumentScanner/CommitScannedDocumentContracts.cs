namespace ERP.API.Contracts.DocumentScanner;

public static class DocumentScanCommitOperations
{
    public const string BuyerDebt = "buyerDebt";
    public const string SupplierPurchaseOrder = "supplierPurchaseOrder";
    public const string StockIn = "stockIn";
}

public sealed class CommitScannedLineItemRequest
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}

public sealed class CommitScannedDocumentRequest
{
    public string Operation { get; init; } = string.Empty;
    public Guid? BuyerCariAccountId { get; init; }
    public Guid? SupplierCariAccountId { get; init; }
    public Guid? WarehouseId { get; init; }
    public string? DocumentDate { get; init; }
    public string? DocumentNumber { get; init; }
    public bool CreateMissingProducts { get; init; } = true;
    public IReadOnlyList<CommitScannedLineItemRequest> Items { get; init; } = [];
}

public sealed class CommitScannedDocumentResponse
{
    public string Operation { get; init; } = string.Empty;
    public int SourceItemCount { get; init; }
    public int ProcessedItemCount { get; init; }
    public int CreatedProductCount { get; init; }
    public IReadOnlyList<Guid> CreatedProductIds { get; init; } = [];
    public IReadOnlyList<Guid> CreatedRecordIds { get; init; } = [];
    public string Message { get; init; } = string.Empty;
}
