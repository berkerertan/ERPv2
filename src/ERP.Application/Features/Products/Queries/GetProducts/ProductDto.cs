namespace ERP.Application.Features.Products.Queries.GetProducts;

public sealed record ProductDto(Guid Id, string Code, string Name, string Unit, string Category);
