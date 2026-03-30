namespace ERP.Application.Abstractions.DocumentScanner;

public sealed record ScanDocumentRequest(
    string ImageBase64,
    string MimeType,
    string Provider);

public sealed record ScannedLineItem(
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal TotalPrice,
    decimal? TaxRate);

public sealed record DocumentScanResult(
    string? VendorName,
    string? VendorTaxId,
    string? DocumentDate,
    string? DocumentNumber,
    string? DocumentType,
    string? Currency,
    IReadOnlyList<ScannedLineItem> Items,
    decimal? Subtotal,
    decimal? TaxAmount,
    decimal? Total,
    string Provider,
    string? ErrorMessage);

public interface IDocumentScannerService
{
    Task<DocumentScanResult> AnalyzeAsync(ScanDocumentRequest request, CancellationToken cancellationToken = default);
}
