using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Common;
using MediatR;

namespace ERP.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.SearchAsync(
            request.Search,
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDir,
            cancellationToken);

        return products
            .Select(x => new ProductDto(
                x.Id,
                x.Code,
                x.Name,
                x.ShortDescription,
                x.Brand,
                x.ProductType,
                x.Unit,
                CsvListSerializer.Deserialize(x.AlternativeUnitsCsv, maxItems: 30),
                x.Category,
                x.SubCategory,
                x.BarcodeEan13,
                CsvListSerializer.Deserialize(x.AlternativeBarcodesCsv, maxItems: 100),
                x.QrCode,
                x.PurchaseVatRate,
                x.SalesVatRate,
                x.IsActive,
                x.MinimumStockLevel,
                x.MaximumStockLevel,
                x.DefaultWarehouseId,
                x.DefaultShelfCode,
                x.ImageUrl,
                x.TechnicalDocumentUrl,
                x.LastPurchasePrice,
                x.LastSalePrice,
                x.DefaultSalePrice,
                x.CriticalStockLevel))
            .ToList();
    }
}
