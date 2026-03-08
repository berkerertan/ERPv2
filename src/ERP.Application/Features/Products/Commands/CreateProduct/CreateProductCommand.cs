using MediatR;

namespace ERP.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Code,
    string Name,
    string Unit,
    string Category,
    string? BarcodeEan13,
    string? QrCode,
    decimal DefaultSalePrice,
    decimal CriticalStockLevel) : IRequest<Guid>;
