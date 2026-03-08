namespace ERP.API.Contracts.Products;

public sealed record UpdateProductRequest(
    string Code,
    string Name,
    string Unit,
    string Category,
    string? BarcodeEan13,
    string? QrCode,
    decimal DefaultSalePrice,
    decimal CriticalStockLevel);
