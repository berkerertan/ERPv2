namespace ERP.Application.Common.Models;

public sealed record CariAccountExcelRow(
    int RowNumber,
    string Code,
    string Name,
    string Phone,
    string Type,
    string RiskLimit,
    string MaturityDays);
