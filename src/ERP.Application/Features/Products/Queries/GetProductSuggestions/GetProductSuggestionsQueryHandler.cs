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

        var products = await productRepository.SuggestAsync(
            request.Search,
            request.Limit,
            cancellationToken);

        return products
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
