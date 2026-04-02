using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ERP.Application.Abstractions.Media;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace ERP.Infrastructure.Media;

public sealed class CloudinaryMediaStorageService(IOptions<CloudinaryOptions> options) : IMediaStorageService
{
    private static readonly Regex PublicIdRegex = new(
        @"/upload/(?:[^/]+/)*(?:v\d+/)?(?<id>.+)\.[a-zA-Z0-9]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly CloudinaryOptions _options = options.Value;

    public bool IsConfigured =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.CloudName) &&
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.ApiSecret);

    public async Task<MediaUploadResult> UploadProductImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Cloudinary is not configured.");
        }

        var cloudinary = CreateClient();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = _options.ProductImageFolder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var uploadResult = await cloudinary.UploadAsync(uploadParams);
        if (uploadResult.Error is not null)
        {
            throw new InvalidOperationException(uploadResult.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(uploadResult.SecureUrl?.AbsoluteUri) || string.IsNullOrWhiteSpace(uploadResult.PublicId))
        {
            throw new InvalidOperationException("Cloudinary upload did not return URL/public id.");
        }

        return new MediaUploadResult(
            uploadResult.SecureUrl.AbsoluteUri,
            uploadResult.PublicId,
            uploadResult.Format,
            uploadResult.Width,
            uploadResult.Height,
            uploadResult.Bytes);
    }

    public async Task<MediaUploadResult> UploadStockMovementProofAsync(
        Stream stream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Cloudinary is not configured.");
        }

        var cloudinary = CreateClient();
        var normalizedContentType = (contentType ?? string.Empty).Trim().ToLowerInvariant();

        if (normalizedContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = _options.StockMovementProofFolder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var imageResult = await cloudinary.UploadAsync(imageParams);
            if (imageResult.Error is not null)
            {
                throw new InvalidOperationException(imageResult.Error.Message);
            }

            if (string.IsNullOrWhiteSpace(imageResult.SecureUrl?.AbsoluteUri) || string.IsNullOrWhiteSpace(imageResult.PublicId))
            {
                throw new InvalidOperationException("Cloudinary upload did not return URL/public id.");
            }

            return new MediaUploadResult(
                imageResult.SecureUrl.AbsoluteUri,
                imageResult.PublicId,
                imageResult.Format,
                imageResult.Width,
                imageResult.Height,
                imageResult.Bytes);
        }

        if (string.Equals(normalizedContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            var rawParams = new RawUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = _options.StockMovementProofFolder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var rawResult = await cloudinary.UploadAsync(rawParams);
            if (rawResult.Error is not null)
            {
                throw new InvalidOperationException(rawResult.Error.Message);
            }

            if (string.IsNullOrWhiteSpace(rawResult.SecureUrl?.AbsoluteUri) || string.IsNullOrWhiteSpace(rawResult.PublicId))
            {
                throw new InvalidOperationException("Cloudinary upload did not return URL/public id.");
            }

            return new MediaUploadResult(
                rawResult.SecureUrl.AbsoluteUri,
                rawResult.PublicId,
                rawResult.Format,
                null,
                null,
                rawResult.Bytes);
        }

        throw new InvalidOperationException("Unsupported proof content type.");
    }

    public async Task DeleteByPublicIdAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var cloudinary = CreateClient();
        var deleteParams = new DeletionParams(publicId) { ResourceType = ResourceType.Image };
        await cloudinary.DestroyAsync(deleteParams);
    }

    public string? TryExtractPublicIdFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var match = PublicIdRegex.Match(url);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["id"].Value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private Cloudinary CreateClient()
    {
        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        return new Cloudinary(account);
    }
}
