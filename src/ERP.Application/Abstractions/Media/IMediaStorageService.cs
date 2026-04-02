namespace ERP.Application.Abstractions.Media;

public sealed record MediaUploadResult(
    string Url,
    string PublicId,
    string? Format,
    int? Width,
    int? Height,
    long? Bytes);

public interface IMediaStorageService
{
    bool IsConfigured { get; }
    Task<MediaUploadResult> UploadProductImageAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<MediaUploadResult> UploadStockMovementProofAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    string? TryExtractPublicIdFromUrl(string? url);
}
