using ERP.Application.Abstractions.Imports;
using ERP.Application.Common.Models;
using ClosedXML.Excel;

namespace ERP.Infrastructure.Imports;

public sealed class ClosedXmlCariAccountExcelReader : ICariAccountExcelReader
{
    public Task<IReadOnlyList<CariAccountExcelRow>> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            throw new InvalidDataException("Excel file has no worksheet.");
        }

        var headerRow = worksheet.Row(1);
        var columnCount = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        if (columnCount == 0)
        {
            throw new InvalidDataException("Excel header row is empty.");
        }

        var columns = ResolveColumns(headerRow, columnCount);

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var rows = new List<CariAccountExcelRow>();

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = worksheet.Row(rowNumber);
            var code = GetCellValue(row, columns.Code);
            var name = GetCellValue(row, columns.Name);
            var type = GetCellValue(row, columns.Type);
            var riskLimit = GetCellValue(row, columns.RiskLimit);
            var maturityDays = GetCellValue(row, columns.MaturityDays);

            if (string.IsNullOrWhiteSpace(code)
                && string.IsNullOrWhiteSpace(name)
                && string.IsNullOrWhiteSpace(type)
                && string.IsNullOrWhiteSpace(riskLimit)
                && string.IsNullOrWhiteSpace(maturityDays))
            {
                continue;
            }

            rows.Add(new CariAccountExcelRow(
                rowNumber,
                code,
                name,
                type,
                riskLimit,
                maturityDays));
        }

        return Task.FromResult<IReadOnlyList<CariAccountExcelRow>>(rows);
    }

    private static (int Code, int Name, int Type, int? RiskLimit, int? MaturityDays) ResolveColumns(IXLRow headerRow, int columnCount)
    {
        var normalized = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var column = 1; column <= columnCount; column++)
        {
            var key = NormalizeHeader(headerRow.Cell(column).GetString());
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            normalized.TryAdd(key, column);
        }

        var code = FindColumn(normalized, "KOD", "CODE", "CARIKOD", "ACCOUNTCODE", "HESAPKOD");
        var name = FindColumn(normalized, "AD", "ADI", "NAME", "UNVAN", "TICARIUNVAN", "ADSOYAD", "CARIADI", "ACCOUNTNAME");
        var type = FindColumn(normalized, "TIP", "TUR", "TYPE", "CARITIP", "CARITUR", "HESAPTIPI");

        if (code is null || name is null || type is null)
        {
            throw new InvalidDataException("Required columns are missing. Required headers: Code, Name, Type.");
        }

        var riskLimit = FindColumn(normalized, "RISKLIMIT", "RISKLIMIT", "RISK", "LIMIT", "RISKLIMITI");
        var maturityDays = FindColumn(normalized, "VADEGUN", "VADEGUNU", "MATURITYDAYS", "MATURITY", "GUN");

        return (code.Value, name.Value, type.Value, riskLimit, maturityDays);
    }

    private static int? FindColumn(Dictionary<string, int> columns, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (columns.TryGetValue(key, out var column))
            {
                return column;
            }
        }

        return null;
    }

    private static string GetCellValue(IXLRow row, int? column)
    {
        if (column is null)
        {
            return string.Empty;
        }

        return row.Cell(column.Value).GetValue<string>().Trim();
    }

    private static string NormalizeHeader(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("Ý", "I")
            .Replace("Ç", "C")
            .Replace("Ð", "G")
            .Replace("Ö", "O")
            .Replace("Þ", "S")
            .Replace("Ü", "U")
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .Replace(".", string.Empty);
    }
}
