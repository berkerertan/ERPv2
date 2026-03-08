using MediatR;

namespace ERP.Application.Features.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest;
