using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Common;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllAsync(cancellationToken);
        IEnumerable<Product> query = products;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Category.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (x.ShortDescription != null && x.ShortDescription.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.SubCategory != null && x.SubCategory.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.Brand != null && x.Brand.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.ProductType != null && x.ProductType.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.BarcodeEan13 != null && x.BarcodeEan13.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.QrCode != null && x.QrCode.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.AlternativeBarcodesCsv != null && x.AlternativeBarcodesCsv.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        query = ApplySort(query, request.SortBy, request.SortDir);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200);

        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

    private static IEnumerable<Product> ApplySort(IEnumerable<Product> query, string? sortBy, string sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var key = sortBy?.Trim().ToLowerInvariant();

        return key switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "category" => desc ? query.OrderByDescending(x => x.Category) : query.OrderBy(x => x.Category),
            "brand" => desc ? query.OrderByDescending(x => x.Brand) : query.OrderBy(x => x.Brand),
            "type" or "producttype" => desc ? query.OrderByDescending(x => x.ProductType) : query.OrderBy(x => x.ProductType),
            "price" or "defaultsaleprice" => desc ? query.OrderByDescending(x => x.DefaultSalePrice) : query.OrderBy(x => x.DefaultSalePrice),
            _ => desc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code)
        };
    }
}
