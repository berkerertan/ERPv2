using MediatR;

namespace ERP.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,
    string SortDir = "asc") : IRequest<IReadOnlyList<ProductDto>>;
