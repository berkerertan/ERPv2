namespace ERP.API.Contracts.Products;

public sealed record CreateProductRequest(string Code, string Name, string Unit, string Category);
