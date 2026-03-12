namespace ERP.API.Contracts.Products;

public sealed class UpdateProductRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = "EA";
    public string? Category { get; init; }
    public string? ShortDescription { get; init; }
    public string? SubCategory { get; init; }
    public string? Brand { get; init; }
    public IReadOnlyList<string>? AlternativeUnits { get; init; }
    public string? BarcodeEan13 { get; init; }
    public IReadOnlyList<string>? AlternativeBarcodes { get; init; }
    public string? QrCode { get; init; }
    public string? ProductType { get; init; }
    public decimal? PurchaseVatRate { get; init; }
    public decimal? SalesVatRate { get; init; }
    public bool IsActive { get; init; } = true;
    public decimal? MinimumStockLevel { get; init; }
    public decimal CriticalStockLevel { get; init; }
    public decimal? MaximumStockLevel { get; init; }
    public Guid? DefaultWarehouseId { get; init; }
    public string? DefaultShelfCode { get; init; }
    public string? ImageUrl { get; init; }
    public string? TechnicalDocumentUrl { get; init; }
    public decimal DefaultSalePrice { get; init; }
    public decimal? LastPurchasePrice { get; init; }
    public decimal? LastSalePrice { get; init; }
}
