using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.Products.Queries.GetProducts;
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
            product.Unit,
            product.Category,
            product.BarcodeEan13,
            product.QrCode,
            product.DefaultSalePrice,
            product.CriticalStockLevel);
    }
}
