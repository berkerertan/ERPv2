using ERP.Application.Abstractions.Imports;
using ERP.Application.Common.Models;
using ClosedXML.Excel;

namespace ERP.Infrastructure.Imports;

public sealed class ClosedXmlCariAccountExcelReader : ICariAccountExcelReader
{
    public Task<IReadOnlyList<CariAccountExcelRow>> ReadAsync(
        Stream stream,
        CariImportColumnMapping? mapping,
        CancellationToken cancellationToken = default)
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

        var columns = ResolveColumns(headerRow, columnCount, mapping);

        if (columns.Name is null)
        {
            throw new InvalidDataException("Required column is missing. Name column could not be resolved.");
        }

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

    private static (int? Code, int? Name, int? Type, int? RiskLimit, int? MaturityDays) ResolveColumns(
        IXLRow headerRow,
        int columnCount,
        CariImportColumnMapping? mapping)
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

        var code = ResolveColumn(
            normalized,
            mapping?.CodeColumn,
            "KOD", "CODE", "CARIKOD", "ACCOUNTCODE", "HESAPKOD");

        var name = ResolveColumn(
            normalized,
            mapping?.NameColumn,
            "MALZEMEACIKLAMA",
            "AD", "ADI", "NAME", "UNVAN", "TICARIUNVAN", "ADSOYAD", "CARIADI", "ACCOUNTNAME");

        var type = ResolveColumn(
            normalized,
            mapping?.TypeColumn,
            "TIP", "TUR", "TYPE", "CARITIP", "CARITUR", "HESAPTIPI");

        var riskLimit = ResolveColumn(
            normalized,
            mapping?.RiskLimitColumn,
            "TOPLAMTUTAR",
            "RISKLIMIT", "RISK", "LIMIT", "RISKLIMITI");

        var maturityDays = ResolveColumn(
            normalized,
            mapping?.MaturityDaysColumn,
            "VADEGUN", "VADEGUNU", "MATURITYDAYS", "MATURITY", "GUN");

        return (code, name, type, riskLimit, maturityDays);
    }

    private static int? ResolveColumn(
        Dictionary<string, int> columns,
        string? explicitHeader,
        params string[] aliases)
    {
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            var explicitKey = NormalizeHeader(explicitHeader);
            if (columns.TryGetValue(explicitKey, out var explicitColumn))
            {
                return explicitColumn;
            }
        }

        foreach (var alias in aliases)
        {
            if (columns.TryGetValue(alias, out var column))
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
