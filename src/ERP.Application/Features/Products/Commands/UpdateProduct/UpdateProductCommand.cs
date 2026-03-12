using MediatR;

namespace ERP.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Code,
    string Name,
    string Unit,
    string? Category,
    string? ShortDescription,
    string? SubCategory,
    string? Brand,
    IReadOnlyList<string>? AlternativeUnits,
    string? BarcodeEan13,
    IReadOnlyList<string>? AlternativeBarcodes,
    string? QrCode,
    string? ProductType,
    decimal? PurchaseVatRate,
    decimal? SalesVatRate,
    bool IsActive,
    decimal? MinimumStockLevel,
    decimal? MaximumStockLevel,
    Guid? DefaultWarehouseId,
    string? DefaultShelfCode,
    string? ImageUrl,
    string? TechnicalDocumentUrl,
    decimal? LastPurchasePrice,
    decimal? LastSalePrice,
    decimal DefaultSalePrice,
    decimal CriticalStockLevel) : IRequest;
