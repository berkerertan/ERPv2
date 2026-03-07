using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await productRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            throw new ConflictException("Product code already exists.");
        }

        var product = new Product
        {
            Code = request.Code,
            Name = request.Name,
            Unit = request.Unit,
            Category = request.Category
        };

        await productRepository.AddAsync(product, cancellationToken);
        return product.Id;
    }
}
