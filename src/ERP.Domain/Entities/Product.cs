using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Product : TenantOwnedEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "EA";
    public string Category { get; set; } = string.Empty;
    public string? BarcodeEan13 { get; set; }
    public string? QrCode { get; set; }
    public decimal DefaultSalePrice { get; set; }
    public decimal CriticalStockLevel { get; set; }
}

