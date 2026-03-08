using MediatR;

namespace ERP.Application.Features.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Code,
    string Name,
    string Unit,
    string Category,
    string? BarcodeEan13,
    string? QrCode,
    decimal DefaultSalePrice,
    decimal CriticalStockLevel) : IRequest;
