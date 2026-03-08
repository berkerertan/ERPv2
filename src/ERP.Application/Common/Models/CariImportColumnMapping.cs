namespace ERP.Application.Common.Models;

public sealed record CariImportColumnMapping(
    string? CodeColumn,
    string? NameColumn,
    string? TypeColumn,
    string? RiskLimitColumn,
    string? MaturityDaysColumn,
    string? DefaultType,
    string? CodePrefix);
