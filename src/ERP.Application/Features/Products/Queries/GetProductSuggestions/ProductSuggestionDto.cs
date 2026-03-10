namespace ERP.Application.Features.Products.Queries.GetProductSuggestions;

public sealed record ProductSuggestionDto(
    Guid Id,
    string Code,
    string Name,
    string Label,
    string? Subtitle);
