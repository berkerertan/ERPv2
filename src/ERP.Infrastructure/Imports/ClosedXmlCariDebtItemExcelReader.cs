using ClosedXML.Excel;
using ERP.Application.Abstractions.Imports;
using ERP.Application.Common.Models;
using System.Globalization;

namespace ERP.Infrastructure.Imports;

public sealed class ClosedXmlCariDebtItemExcelReader : ICariDebtItemExcelReader
{
    public Task<IReadOnlyList<CariDebtItemExcelRow>> ReadAsync(
        Stream stream,
        CariDebtItemImportColumnMapping? mapping,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            throw new InvalidDataException("Excel file has no worksheet.");
        }

        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRowNumber == 0)
        {
            throw new InvalidDataException("Excel worksheet is empty.");
        }

        var (headerRowNumber, columns) = ResolveHeaderAndColumns(worksheet, lastRowNumber, mapping);

        if (columns.TransactionDate is null || columns.MaterialDescription is null)
        {
            throw new InvalidDataException("Required columns are missing. TransactionDate and MaterialDescription are required.");
        }

        var rows = new List<CariDebtItemExcelRow>();

        for (var rowNumber = headerRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = worksheet.Row(rowNumber);

            var transactionDate = GetCellValue(row, columns.TransactionDate, isDateColumn: true);
            var materialDescription = GetCellValue(row, columns.MaterialDescription, isDateColumn: false);
            var quantity = GetCellValue(row, columns.Quantity, isDateColumn: false);
            var listPrice = GetCellValue(row, columns.ListPrice, isDateColumn: false);
            var salePrice = GetCellValue(row, columns.SalePrice, isDateColumn: false);
            var totalAmount = GetCellValue(row, columns.TotalAmount, isDateColumn: false);
            var payment = GetCellValue(row, columns.Payment, isDateColumn: false);
            var remainingBalance = GetCellValue(row, columns.RemainingBalance, isDateColumn: false);

            if (string.IsNullOrWhiteSpace(transactionDate)
                && string.IsNullOrWhiteSpace(materialDescription)
                && string.IsNullOrWhiteSpace(quantity)
                && string.IsNullOrWhiteSpace(listPrice)
                && string.IsNullOrWhiteSpace(salePrice)
                && string.IsNullOrWhiteSpace(totalAmount)
                && string.IsNullOrWhiteSpace(payment)
                && string.IsNullOrWhiteSpace(remainingBalance))
            {
                continue;
            }

            rows.Add(new CariDebtItemExcelRow(
                rowNumber,
                transactionDate,
                materialDescription,
                quantity,
                listPrice,
                salePrice,
                totalAmount,
                payment,
                remainingBalance));
        }

        return Task.FromResult<IReadOnlyList<CariDebtItemExcelRow>>(rows);
    }

    private static (int HeaderRowNumber, (int? TransactionDate, int? MaterialDescription, int? Quantity, int? ListPrice, int? SalePrice, int? TotalAmount, int? Payment, int? RemainingBalance) Columns)
        ResolveHeaderAndColumns(IXLWorksheet worksheet, int lastRowNumber, CariDebtItemImportColumnMapping? mapping)
    {
        var maxHeaderScanRow = Math.Min(lastRowNumber, 50);

        var bestHeaderRow = 1;
        var bestColumns = default((int? TransactionDate, int? MaterialDescription, int? Quantity, int? ListPrice, int? SalePrice, int? TotalAmount, int? Payment, int? RemainingBalance));
        var bestScore = -1;
        var bestHasRequired = false;

        for (var rowNumber = 1; rowNumber <= maxHeaderScanRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            var columnCount = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (columnCount == 0)
            {
                continue;
            }

            var columns = ResolveColumns(row, columnCount, mapping);
            var score = CountResolvedColumns(columns);
            var hasRequired = columns.TransactionDate is not null && columns.MaterialDescription is not null;

            if (score == 0)
            {
                continue;
            }

            if (hasRequired && (!bestHasRequired || score > bestScore))
            {
                bestHeaderRow = rowNumber;
                bestColumns = columns;
                bestScore = score;
                bestHasRequired = true;
                continue;
            }

            if (!bestHasRequired && score > bestScore)
            {
                bestHeaderRow = rowNumber;
                bestColumns = columns;
                bestScore = score;
            }
        }

        if (bestScore < 0)
        {
            throw new InvalidDataException("Excel header row could not be resolved.");
        }

        return (bestHeaderRow, bestColumns);
    }

    private static int CountResolvedColumns((int? TransactionDate, int? MaterialDescription, int? Quantity, int? ListPrice, int? SalePrice, int? TotalAmount, int? Payment, int? RemainingBalance) columns)
    {
        var count = 0;

        if (columns.TransactionDate is not null) count++;
        if (columns.MaterialDescription is not null) count++;
        if (columns.Quantity is not null) count++;
        if (columns.ListPrice is not null) count++;
        if (columns.SalePrice is not null) count++;
        if (columns.TotalAmount is not null) count++;
        if (columns.Payment is not null) count++;
        if (columns.RemainingBalance is not null) count++;

        return count;
    }

    private static (int? TransactionDate, int? MaterialDescription, int? Quantity, int? ListPrice, int? SalePrice, int? TotalAmount, int? Payment, int? RemainingBalance) ResolveColumns(
        IXLRow headerRow,
        int columnCount,
        CariDebtItemImportColumnMapping? mapping)
    {
        var normalized = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var column = 1; column <= columnCount; column++)
        {
            var key = NormalizeHeader(headerRow.Cell(column).GetValue<string>());
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            normalized.TryAdd(key, column);
        }

        var transactionDate = ResolveColumn(normalized, mapping?.TransactionDateColumn, columnCount, "TARIH", "TARIHI", "DATE", "ISLEMTARIHI", "ISLEMTARIH");
        var materialDescription = ResolveColumn(normalized, mapping?.MaterialDescriptionColumn, columnCount, "MALZEMEACIKLAMA", "MALZEME", "ACIKLAMA", "MATERIAL", "DESCRIPTION", "URUN", "URUNADI");
        var quantity = ResolveColumn(normalized, mapping?.QuantityColumn, columnCount, "ADET", "ADEDI", "MIKTAR", "MIKTARI", "QUANTITY", "QTY");
        var listPrice = ResolveColumn(normalized, mapping?.ListPriceColumn, columnCount, "LISTEFIYATI", "LISTPRICE", "LISTE", "LISTFIYAT", "BIRIMLISTEFIYATI");
        var salePrice = ResolveColumn(normalized, mapping?.SalePriceColumn, columnCount, "SATISFIYATI", "SALEPRICE", "SATIS", "BIRIMFIYAT", "BIRIMSATISFIYATI", "BIRIMSATIS");
        var totalAmount = ResolveColumn(normalized, mapping?.TotalAmountColumn, columnCount, "TOPLAMTUTAR", "TOPLAM", "TOTAL", "AMOUNT", "TUTAR", "NETTUTAR");
        var payment = ResolveColumn(normalized, mapping?.PaymentColumn, columnCount, "ODEME", "PAYMENT", "TAHSILAT", "ODENEN");
        var remainingBalance = ResolveColumn(normalized, mapping?.RemainingBalanceColumn, columnCount, "KALANBAKIYE", "BAKIYE", "BALANCE", "REMAINING", "KALAN", "DEVIRBAKIYE");

        return (transactionDate, materialDescription, quantity, listPrice, salePrice, totalAmount, payment, remainingBalance);
    }

    private static int? ResolveColumn(Dictionary<string, int> columns, string? explicitHeader, int columnCount, params string[] aliases)
    {
        if (!string.IsNullOrWhiteSpace(explicitHeader))
        {
            if (TryParseColumnReference(explicitHeader, out var explicitColumn) && explicitColumn <= columnCount)
            {
                return explicitColumn;
            }

            var explicitKey = NormalizeHeader(explicitHeader);
            if (columns.TryGetValue(explicitKey, out var resolvedColumn))
            {
                return resolvedColumn;
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

    private static bool TryParseColumnReference(string value, out int columnNumber)
    {
        columnNumber = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) && numeric > 0)
        {
            columnNumber = numeric;
            return true;
        }

        var letters = trimmed.Replace("$", string.Empty).ToUpperInvariant();
        if (letters.Length == 0)
        {
            return false;
        }

        foreach (var ch in letters)
        {
            if (ch < 'A' || ch > 'Z')
            {
                return false;
            }

            columnNumber = (columnNumber * 26) + (ch - 'A' + 1);
        }

        return columnNumber > 0;
    }

    private static string GetCellValue(IXLRow row, int? column, bool isDateColumn)
    {
        if (column is null)
        {
            return string.Empty;
        }

        var cell = row.Cell(column.Value);
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (isDateColumn)
        {
            if (cell.DataType == XLDataType.DateTime)
            {
                return cell.GetDateTime().Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (cell.TryGetValue<DateTime>(out var parsedDate))
            {
                return parsedDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        if (cell.DataType == XLDataType.Number)
        {
            var formatted = cell.GetFormattedString().Trim();
            if (!string.IsNullOrWhiteSpace(formatted))
            {
                return formatted;
            }

            return cell.GetDouble().ToString("G17", CultureInfo.InvariantCulture);
        }

        if (cell.HasFormula)
        {
            var formulaValue = cell.GetFormattedString().Trim();
            if (!string.IsNullOrWhiteSpace(formulaValue))
            {
                return formulaValue;
            }
        }

        return cell.GetValue<string>().Trim();
    }

    private static string NormalizeHeader(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("İ", "I")
            .Replace("Ç", "C")
            .Replace("Ğ", "G")
            .Replace("Ö", "O")
            .Replace("Ş", "S")
            .Replace("Ü", "U")
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .Replace(".", string.Empty)
            .Replace(":", string.Empty)
            .Replace(";", string.Empty)
            .Replace("/", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\t", string.Empty);
    }
}
