using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.Products.Queries.GetProductSuggestions;

public sealed class GetProductSuggestionsQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductSuggestionsQuery, IReadOnlyList<ProductSuggestionDto>>
{
    public async Task<IReadOnlyList<ProductSuggestionDto>> Handle(GetProductSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Search))
        {
            return [];
        }

        var search = request.Search.Trim();
        var limit = request.Limit <= 0 ? 8 : Math.Min(request.Limit, 20);

        var products = await productRepository.GetAllAsync(cancellationToken);

        return products
            .Where(x =>
                x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (x.ShortDescription != null && x.ShortDescription.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.Brand != null && x.Brand.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.SubCategory != null && x.SubCategory.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.BarcodeEan13 != null && x.BarcodeEan13.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.AlternativeBarcodesCsv != null && x.AlternativeBarcodesCsv.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (x.QrCode != null && x.QrCode.Contains(search, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(x => x.Code.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(x => x.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Code)
            .Take(limit)
            .Select(x => new ProductSuggestionDto(
                x.Id,
                x.Code,
                x.Name,
                $"{x.Code} - {x.Name}",
                BuildSubtitle(x.Category, x.Brand, x.ProductType)))
            .ToList();
    }

    private static string? BuildSubtitle(string category, string? brand, string? productType)
    {
        var tokens = new[] { category, brand, productType }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return tokens.Length == 0 ? null : string.Join(" | ", tokens);
    }
}
