namespace ERP.API.Contracts.Products;

public sealed record ProductScanResponse(
    string Barcode,
    bool Found,
    ProductScanMatchDto? Product,
    ProductScanDraftDto? Draft);

public sealed record ProductScanMatchDto(
    Guid Id,
    string Code,
    string Name,
    string? BarcodeEan13,
    string? QrCode,
    decimal DefaultSalePrice,
    string Unit,
    string? ImageUrl,
    bool IsActive);

public sealed record ProductScanDraftDto(
    string Code,
    string Name,
    string Unit,
    string Category,
    string? BarcodeEan13,
    string? QrCode,
    decimal DefaultSalePrice,
    decimal CriticalStockLevel);
