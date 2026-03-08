using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(IProductRepository productRepository)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        if (await productRepository.GetByIdAsync(request.ProductId, cancellationToken) is null)
        {
            throw new NotFoundException("Product not found.");
        }

        await productRepository.DeleteAsync(request.ProductId, cancellationToken);
    }
}
