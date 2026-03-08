using ERP.Application.Features.Products.Queries.GetProducts;
using MediatR;

namespace ERP.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;
