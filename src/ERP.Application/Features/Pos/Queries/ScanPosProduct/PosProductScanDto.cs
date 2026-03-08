namespace ERP.Application.Features.Pos.Queries.ScanPosProduct;

public sealed record PosProductScanDto(
    Guid ProductId,
    string Code,
    string Name,
    string Unit,
    string Category,
    string? BarcodeEan13,
    string? QrCode,
    decimal DefaultSalePrice,
    decimal AvailableStock);
