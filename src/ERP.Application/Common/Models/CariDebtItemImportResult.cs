namespace ERP.Application.Common.Models;

public sealed record CariDebtItemImportResult(
    int TotalRows,
    int CreatedCount,
    int FailedCount,
    IReadOnlyList<string> Errors);
