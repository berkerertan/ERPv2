using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllAsync(cancellationToken);

        return products
            .OrderBy(x => x.Code)
            .Select(x => new ProductDto(x.Id, x.Code, x.Name, x.Unit, x.Category))
            .ToList();
    }
}
