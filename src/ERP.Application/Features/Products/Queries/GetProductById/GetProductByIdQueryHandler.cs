using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.Products.Queries.GetProducts;
using ERP.Domain.Common;
using MediatR;

namespace ERP.Application.Features.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        return new ProductDto(
            product.Id,
            product.Code,
            product.Name,
            product.ShortDescription,
            product.Brand,
            product.ProductType,
            product.Unit,
            CsvListSerializer.Deserialize(product.AlternativeUnitsCsv, maxItems: 30),
            product.Category,
            product.SubCategory,
            product.BarcodeEan13,
            CsvListSerializer.Deserialize(product.AlternativeBarcodesCsv, maxItems: 100),
            product.QrCode,
            product.PurchaseVatRate,
            product.SalesVatRate,
            product.IsActive,
            product.MinimumStockLevel,
            product.MaximumStockLevel,
            product.DefaultWarehouseId,
            product.DefaultShelfCode,
            product.ImageUrl,
            product.TechnicalDocumentUrl,
            product.LastPurchasePrice,
            product.LastSalePrice,
            product.DefaultSalePrice,
            product.CriticalStockLevel);
    }
}
