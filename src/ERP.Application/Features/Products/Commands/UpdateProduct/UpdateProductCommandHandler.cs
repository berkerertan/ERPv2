using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        var codeOwner = await productRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != product.Id)
        {
            throw new ConflictException("Product code already exists.");
        }

        var normalizedEan13 = NormalizeNullable(request.BarcodeEan13);
        if (!string.IsNullOrWhiteSpace(normalizedEan13))
        {
            var barcodeOwner = await productRepository.GetByBarcodeAsync(normalizedEan13, cancellationToken);
            if (barcodeOwner is not null && barcodeOwner.Id != product.Id)
            {
                throw new ConflictException("EAN-13 barcode already exists.");
            }
        }

        var normalizedQrCode = NormalizeNullable(request.QrCode);
        if (!string.IsNullOrWhiteSpace(normalizedQrCode))
        {
            var qrOwner = await productRepository.GetByBarcodeAsync(normalizedQrCode, cancellationToken);
            if (qrOwner is not null && qrOwner.Id != product.Id)
            {
                throw new ConflictException("QR code already exists.");
            }
        }

        product.Code = request.Code;
        product.Name = request.Name;
        product.Unit = request.Unit;
        product.Category = request.Category;
        product.BarcodeEan13 = normalizedEan13;
        product.QrCode = normalizedQrCode;
        product.DefaultSalePrice = request.DefaultSalePrice;
        product.CriticalStockLevel = request.CriticalStockLevel;

        await productRepository.UpdateAsync(product, cancellationToken);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
