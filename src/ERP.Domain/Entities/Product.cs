using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Product : TenantOwnedEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string Unit { get; set; } = "EA";
    public string? AlternativeUnitsCsv { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? SubCategory { get; set; }
    public string? Brand { get; set; }
    public string? BarcodeEan13 { get; set; }
    public string? AlternativeBarcodesCsv { get; set; }
    public string? QrCode { get; set; }
    public string? ProductType { get; set; }
    public decimal? PurchaseVatRate { get; set; }
    public decimal? SalesVatRate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? MinimumStockLevel { get; set; }
    public decimal DefaultSalePrice { get; set; }
    public decimal CriticalStockLevel { get; set; }
    public decimal? MaximumStockLevel { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public string? DefaultShelfCode { get; set; }
    public string? ImageUrl { get; set; }
    public string? TechnicalDocumentUrl { get; set; }
    public decimal? LastPurchasePrice { get; set; }
    public decimal? LastSalePrice { get; set; }
}

