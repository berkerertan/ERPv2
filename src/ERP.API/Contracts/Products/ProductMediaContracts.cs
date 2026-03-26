namespace ERP.API.Contracts.Products;

public sealed class ProductImageUploadForm
{
    public IFormFile? File { get; init; }
}

public sealed record ProductImageUploadResponse(
    Guid ProductId,
    string ImageUrl,
    string PublicId,
    string? Format,
    int? Width,
    int? Height,
    long? Bytes);
