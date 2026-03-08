namespace ERP.Application.Common.Models;

public sealed record CariAccountImportResult(
    int TotalRows,
    int CreatedCount,
    int UpdatedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<string> Errors);
