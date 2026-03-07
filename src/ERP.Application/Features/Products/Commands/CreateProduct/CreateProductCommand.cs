using MediatR;

namespace ERP.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(string Code, string Name, string Unit, string Category) : IRequest<Guid>;
