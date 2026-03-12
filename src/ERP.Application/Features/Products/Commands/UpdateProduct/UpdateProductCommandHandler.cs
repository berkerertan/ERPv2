using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Common;
using MediatR;

namespace ERP.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        var code = NormalizeRequired(request.Code);
        var codeOwner = await productRepository.GetByCodeAsync(code, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != product.Id)
        {
            throw new ConflictException("Product code already exists.");
        }

        var normalizedEan13 = NormalizeNullable(request.BarcodeEan13);
        var normalizedQrCode = NormalizeNullable(request.QrCode);
        var alternativeBarcodes = CsvListSerializer.Deserialize(
            CsvListSerializer.Serialize(request.AlternativeBarcodes, maxItems: 100, maxItemLength: 80),
            maxItems: 100);

        var barcodeCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(normalizedEan13))
        {
            barcodeCandidates.Add(normalizedEan13);
        }

        if (!string.IsNullOrWhiteSpace(normalizedQrCode))
        {
            barcodeCandidates.Add(normalizedQrCode);
        }

        foreach (var barcode in alternativeBarcodes)
        {
            barcodeCandidates.Add(barcode);
        }

        foreach (var barcode in barcodeCandidates)
        {
            var barcodeOwner = await productRepository.GetByBarcodeAsync(barcode, cancellationToken);
            if (barcodeOwner is not null && barcodeOwner.Id != product.Id)
            {
                throw new ConflictException($"Barcode already exists: {barcode}");
            }
        }

        product.Code = code;
        product.Name = NormalizeRequired(request.Name);
        product.Unit = NormalizeOrDefault(request.Unit, "EA");
        product.Category = NormalizeOrDefault(request.Category, string.Empty);
        product.ShortDescription = NormalizeNullable(request.ShortDescription);
        product.SubCategory = NormalizeNullable(request.SubCategory);
        product.Brand = NormalizeNullable(request.Brand);
        product.AlternativeUnitsCsv = CsvListSerializer.Serialize(request.AlternativeUnits, maxItems: 30, maxItemLength: 30);
        product.BarcodeEan13 = normalizedEan13;
        product.AlternativeBarcodesCsv = CsvListSerializer.Serialize(alternativeBarcodes, maxItems: 100, maxItemLength: 80);
        product.QrCode = normalizedQrCode;
        product.ProductType = NormalizeNullable(request.ProductType);
        product.PurchaseVatRate = request.PurchaseVatRate;
        product.SalesVatRate = request.SalesVatRate;
        product.IsActive = request.IsActive;
        product.MinimumStockLevel = request.MinimumStockLevel;
        product.MaximumStockLevel = request.MaximumStockLevel;
        product.DefaultWarehouseId = request.DefaultWarehouseId;
        product.DefaultShelfCode = NormalizeNullable(request.DefaultShelfCode);
        product.ImageUrl = NormalizeNullable(request.ImageUrl);
        product.TechnicalDocumentUrl = NormalizeNullable(request.TechnicalDocumentUrl);
        product.LastPurchasePrice = request.LastPurchasePrice;
        product.LastSalePrice = request.LastSalePrice;
        product.DefaultSalePrice = request.DefaultSalePrice;
        product.CriticalStockLevel = request.CriticalStockLevel;

        await productRepository.UpdateAsync(product, cancellationToken);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }
}
