using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await productRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            throw new ConflictException("Product code already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.BarcodeEan13)
            && await productRepository.GetByBarcodeAsync(request.BarcodeEan13, cancellationToken) is not null)
        {
            throw new ConflictException("EAN-13 barcode already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.QrCode)
            && await productRepository.GetByBarcodeAsync(request.QrCode, cancellationToken) is not null)
        {
            throw new ConflictException("QR code already exists.");
        }

        var product = new Product
        {
            Code = request.Code,
            Name = request.Name,
            Unit = request.Unit,
            Category = request.Category,
            BarcodeEan13 = NormalizeNullable(request.BarcodeEan13),
            QrCode = NormalizeNullable(request.QrCode),
            DefaultSalePrice = request.DefaultSalePrice,
            CriticalStockLevel = request.CriticalStockLevel
        };

        await productRepository.AddAsync(product, cancellationToken);
        return product.Id;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
