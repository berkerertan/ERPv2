namespace ERP.API.Contracts.StockMovements;

public sealed record StockMovementProofUploadResponse(
    string Url,
    string PublicId,
    string? Format,
    long? Bytes);
