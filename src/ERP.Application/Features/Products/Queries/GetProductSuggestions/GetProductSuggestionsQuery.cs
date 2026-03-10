using MediatR;

namespace ERP.Application.Features.Products.Queries.GetProductSuggestions;

public sealed record GetProductSuggestionsQuery(string? Search, int Limit = 8)
    : IRequest<IReadOnlyList<ProductSuggestionDto>>;
