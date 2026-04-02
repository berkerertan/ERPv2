namespace ERP.Infrastructure.Media;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    public bool Enabled { get; init; }
    public string CloudName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public string ProductImageFolder { get; init; } = "stoknet/products";
    public string StockMovementProofFolder { get; init; } = "stoknet/stock-proofs";
}
