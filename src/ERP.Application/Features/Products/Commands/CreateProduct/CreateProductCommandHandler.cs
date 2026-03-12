using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Common;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var code = NormalizeRequired(request.Code);
        if (await productRepository.GetByCodeAsync(code, cancellationToken) is not null)
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
            if (await productRepository.GetByBarcodeAsync(barcode, cancellationToken) is not null)
            {
                throw new ConflictException($"Barcode already exists: {barcode}");
            }
        }

        var product = new Product
        {
            Code = code,
            Name = NormalizeRequired(request.Name),
            Unit = NormalizeOrDefault(request.Unit, "EA"),
            Category = NormalizeOrDefault(request.Category, string.Empty),
            ShortDescription = NormalizeNullable(request.ShortDescription),
            SubCategory = NormalizeNullable(request.SubCategory),
            Brand = NormalizeNullable(request.Brand),
            AlternativeUnitsCsv = CsvListSerializer.Serialize(request.AlternativeUnits, maxItems: 30, maxItemLength: 30),
            BarcodeEan13 = normalizedEan13,
            AlternativeBarcodesCsv = CsvListSerializer.Serialize(alternativeBarcodes, maxItems: 100, maxItemLength: 80),
            QrCode = normalizedQrCode,
            ProductType = NormalizeNullable(request.ProductType),
            PurchaseVatRate = request.PurchaseVatRate,
            SalesVatRate = request.SalesVatRate,
            IsActive = request.IsActive,
            MinimumStockLevel = request.MinimumStockLevel,
            MaximumStockLevel = request.MaximumStockLevel,
            DefaultWarehouseId = request.DefaultWarehouseId,
            DefaultShelfCode = NormalizeNullable(request.DefaultShelfCode),
            ImageUrl = NormalizeNullable(request.ImageUrl),
            TechnicalDocumentUrl = NormalizeNullable(request.TechnicalDocumentUrl),
            LastPurchasePrice = request.LastPurchasePrice,
            LastSalePrice = request.LastSalePrice,
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

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }
}
