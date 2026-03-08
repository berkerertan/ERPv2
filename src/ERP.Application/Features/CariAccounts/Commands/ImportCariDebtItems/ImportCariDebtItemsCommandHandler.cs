using ERP.Application.Abstractions.Imports;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Models;
using ERP.Domain.Entities;
using FluentValidation;
using MediatR;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ERP.Application.Features.CariAccounts.Commands.ImportCariDebtItems;

public sealed class ImportCariDebtItemsCommandHandler(
    ICariAccountRepository cariAccountRepository,
    ICariDebtItemRepository debtItemRepository,
    ICariDebtItemExcelReader excelReader)
    : IRequestHandler<ImportCariDebtItemsCommand, CariDebtItemImportResult>
{
    private const decimal MaxDecimal182 = 9999999999999999.99m;
    private const decimal MaxDecimal183 = 999999999999999.999m;
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    public async Task<CariDebtItemImportResult> Handle(ImportCariDebtItemsCommand request, CancellationToken cancellationToken)
    {
        var account = await cariAccountRepository.GetByIdAsync(request.CariAccountId, cancellationToken)
            ?? throw new ValidationException("Cari account not found.");

        IReadOnlyList<CariDebtItemExcelRow> rows;
        try
        {
            using var stream = new MemoryStream(request.FileContent, writable: false);
            rows = await excelReader.ReadAsync(stream, request.ColumnMapping, cancellationToken);
        }
        catch (InvalidDataException ex)
        {
            throw new ValidationException(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ValidationException("Excel file could not be read. Please upload a valid .xlsx file.");
        }

        if (request.ReplaceExisting)
        {
            var existingItems = await debtItemRepository.GetByCariAccountIdAsync(request.CariAccountId, cancellationToken);
            foreach (var existingItem in existingItems)
            {
                await debtItemRepository.DeleteAsync(existingItem.Id, cancellationToken);
            }
        }

        var created = 0;
        var errors = new List<string>();
        DateTime? lastTransactionDate = null;

        foreach (var row in rows)
        {
            if (ShouldSkipRow(row))
            {
                continue;
            }

            TryResolveTransactionDate(row.TransactionDate, lastTransactionDate, out var transactionDate);
            lastTransactionDate = transactionDate;

            var materialDescription = NormalizeText(row.MaterialDescription);
            if (string.IsNullOrWhiteSpace(materialDescription))
            {
                if (IsAllNumericEffectivelyZero(row))
                {
                    continue;
                }

                errors.Add($"Row {row.RowNumber}: Material description is required.");
                continue;
            }

            if (materialDescription.Length > 250)
            {
                materialDescription = materialDescription[..250];
            }

            var quantity = ParseDecimalOrZero(row.Quantity, scale: 3);
            var listPrice = ParseDecimalOrZero(row.ListPrice, scale: 2);
            var salePrice = ParseDecimalOrZero(row.SalePrice, scale: 2);
            var totalAmount = ParseDecimalOrZero(row.TotalAmount, scale: 2);
            var payment = ParseDecimalOrZero(row.Payment, scale: 2);
            var remainingBalance = ParseDecimalOrZero(row.RemainingBalance, scale: 2);

            if (quantity < 0 || listPrice < 0 || salePrice < 0 || totalAmount < 0 || payment < 0 || remainingBalance < 0)
            {
                errors.Add($"Row {row.RowNumber}: Numeric values cannot be negative.");
                continue;
            }

            if (!IsValidDecimal183(quantity)
                || !IsValidDecimal182(listPrice)
                || !IsValidDecimal182(salePrice)
                || !IsValidDecimal182(totalAmount)
                || !IsValidDecimal182(payment)
                || !IsValidDecimal182(remainingBalance))
            {
                errors.Add($"Row {row.RowNumber}: One or more numeric values exceed supported precision.");
                continue;
            }

            var item = new CariDebtItem
            {
                CariAccountId = request.CariAccountId,
                TransactionDate = transactionDate,
                MaterialDescription = materialDescription,
                Quantity = quantity,
                ListPrice = listPrice,
                SalePrice = salePrice,
                TotalAmount = totalAmount,
                Payment = payment,
                RemainingBalance = remainingBalance
            };

            try
            {
                await debtItemRepository.AddAsync(item, cancellationToken);
                created++;
            }
            catch (Exception)
            {
                errors.Add($"Row {row.RowNumber}: Could not persist row due database constraint.");
            }
        }

        var latestItem = (await debtItemRepository.GetByCariAccountIdAsync(request.CariAccountId, cancellationToken)).FirstOrDefault();
        account.CurrentBalance = latestItem?.RemainingBalance ?? 0m;
        await cariAccountRepository.UpdateAsync(account, cancellationToken);

        return new CariDebtItemImportResult(rows.Count, created, errors.Count, errors);
    }

    private static bool ShouldSkipRow(CariDebtItemExcelRow row)
    {
        var description = NormalizeText(row.MaterialDescription);

        if (string.IsNullOrWhiteSpace(row.TransactionDate)
            && string.IsNullOrWhiteSpace(description)
            && string.IsNullOrWhiteSpace(row.Quantity)
            && string.IsNullOrWhiteSpace(row.ListPrice)
            && string.IsNullOrWhiteSpace(row.SalePrice)
            && string.IsNullOrWhiteSpace(row.TotalAmount)
            && string.IsNullOrWhiteSpace(row.Payment)
            && string.IsNullOrWhiteSpace(row.RemainingBalance))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(row.TransactionDate) && string.IsNullOrWhiteSpace(description))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(row.TransactionDate) && IsSummaryDescription(description))
        {
            return true;
        }

        return false;
    }

    private static bool IsAllNumericEffectivelyZero(CariDebtItemExcelRow row)
    {
        return ParseDecimalOrZero(row.Quantity, 3) == 0m
            && ParseDecimalOrZero(row.ListPrice, 2) == 0m
            && ParseDecimalOrZero(row.SalePrice, 2) == 0m
            && ParseDecimalOrZero(row.TotalAmount, 2) == 0m
            && ParseDecimalOrZero(row.Payment, 2) == 0m
            && ParseDecimalOrZero(row.RemainingBalance, 2) == 0m;
    }

    private static bool TryResolveTransactionDate(string rawValue, DateTime? lastTransactionDate, out DateTime transactionDate)
    {
        if (TryParseDate(rawValue, out transactionDate))
        {
            return true;
        }

        if (TryParseDateFromMixedText(rawValue, out transactionDate))
        {
            return true;
        }

        if (lastTransactionDate.HasValue)
        {
            transactionDate = lastTransactionDate.Value;
            return true;
        }

        transactionDate = DateTime.UtcNow.Date;
        return true;
    }

    private static decimal ParseDecimalOrZero(string value, int scale)
    {
        if (!TryParseDecimalRaw(value, scale, out var result))
        {
            return 0m;
        }

        return decimal.Round(result, scale, MidpointRounding.AwayFromZero);
    }

    private static bool TryParseDecimalRaw(string value, int scale, out decimal result)
    {
        result = 0m;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = NormalizeNumericValue(value, out var isNegative);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        if (TryParseFraction(normalized, out result))
        {
            if (isNegative)
            {
                result = -result;
            }

            return true;
        }

        var normalizedNumber = NormalizeSeparators(normalized, scale);
        if (!TryParseDecimalWithCulture(normalizedNumber, CultureInfo.InvariantCulture, out result)
            && !TryParseDecimalWithCulture(normalizedNumber, TrCulture, out result))
        {
            return false;
        }

        if (isNegative)
        {
            result = -result;
        }

        return true;
    }

    private static bool TryParseDecimalWithCulture(string value, CultureInfo culture, out decimal result)
    {
        return decimal.TryParse(
            value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint,
            culture,
            out result);
    }

    private static bool TryParseFraction(string value, out decimal result)
    {
        result = 0m;

        var slashCount = value.Count(c => c == '/');
        if (slashCount != 1)
        {
            return false;
        }

        var parts = value.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!TryParseDecimalWithCulture(parts[0], CultureInfo.InvariantCulture, out var numerator)
            && !TryParseDecimalWithCulture(parts[0], TrCulture, out numerator))
        {
            return false;
        }

        if (!TryParseDecimalWithCulture(parts[1], CultureInfo.InvariantCulture, out var denominator)
            && !TryParseDecimalWithCulture(parts[1], TrCulture, out denominator))
        {
            return false;
        }

        if (denominator == 0m)
        {
            return false;
        }

        result = numerator / denominator;
        return true;
    }

    private static string NormalizeNumericValue(string value, out bool isNegative)
    {
        isNegative = false;

        var normalized = value.Trim();
        if (normalized.StartsWith('(') && normalized.EndsWith(')') && normalized.Length >= 2)
        {
            isNegative = true;
            normalized = normalized[1..^1];
        }

        normalized = normalized
            .Replace("\u00A0", string.Empty)
            .Replace("\u202F", string.Empty)
            .Replace("\u2009", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("'", string.Empty)
            .Replace("’", string.Empty)
            .Replace("`", string.Empty)
            .Replace("₺", string.Empty)
            .Replace("TL", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("TRY", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("USD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("EUR", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("$", string.Empty)
            .Replace("€", string.Empty)
            .Replace("£", string.Empty)
            .Trim();

        return normalized;
    }

    private static string NormalizeSeparators(string value, int scale)
    {
        var commaCount = value.Count(c => c == ',');
        var dotCount = value.Count(c => c == '.');

        if (commaCount > 0 && dotCount > 0)
        {
            var lastComma = value.LastIndexOf(',');
            var lastDot = value.LastIndexOf('.');
            var decimalSeparator = lastComma > lastDot ? ',' : '.';
            var thousandSeparator = decimalSeparator == ',' ? '.' : ',';

            var compact = value.Replace(thousandSeparator.ToString(), string.Empty);
            return decimalSeparator == ',' ? compact.Replace(',', '.') : compact;
        }

        var separator = commaCount > 0 ? ',' : dotCount > 0 ? '.' : '\0';
        if (separator == '\0')
        {
            return value;
        }

        var parts = value.Split(separator);
        if (parts.Length <= 1)
        {
            return value;
        }

        if (parts.Length > 2)
        {
            var allTrailingGroupsAreThreeDigits = parts.Skip(1).All(part => part.Length == 3);
            if (allTrailingGroupsAreThreeDigits)
            {
                return string.Concat(parts);
            }

            var integerPart = string.Concat(parts.Take(parts.Length - 1));
            var fractionPart = parts[^1];
            if (fractionPart.Length == 0)
            {
                return integerPart;
            }

            return string.Concat(integerPart, ".", fractionPart);
        }

        var fractionalPart = parts[1];
        if (fractionalPart.Length == 0)
        {
            return parts[0];
        }

        if (fractionalPart.Length > scale && fractionalPart.Length == 3)
        {
            return string.Concat(parts);
        }

        if (separator == ',')
        {
            return string.Concat(parts[0], ".", fractionalPart);
        }

        return value;
    }
    private static bool TryParseDateFromMixedText(string value, out DateTime result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var text = value.Trim();

        var dateMatches = Regex.Matches(text, @"\d{4}[./-]\d{1,2}[./-]\d{1,2}|\d{1,2}[./-]\d{1,2}[./-]\d{2,4}");
        foreach (Match match in dateMatches)
        {
            if (TryParseDate(match.Value, out result))
            {
                return true;
            }
        }

        var tokens = text.Split(new[] { ' ', '\t', '|', ';', ',', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            var cleaned = token.Trim('.', ',', ';', '-', '_');
            if (TryParseDate(cleaned, out result))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseDate(string value, out DateTime result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        var text = value.Trim();

        var exactFormats = new[]
        {
            "yyyy-MM-dd",
            "dd.MM.yyyy",
            "d.M.yyyy",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "yyyy/MM/dd",
            "dd-MM-yyyy",
            "d-M-yyyy"
        };

        if (DateTime.TryParseExact(text, exactFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateTime.TryParseExact(text, exactFormats, TrCulture, DateTimeStyles.None, out result)
            || DateTime.TryParse(text, TrCulture, DateTimeStyles.None, out result)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            result = result.Date;
            return true;
        }

        if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var oaDate)
            && oaDate > 0
            && oaDate < 2958465)
        {
            try
            {
                result = DateTime.FromOADate(oaDate).Date;
                return true;
            }
            catch (ArgumentException)
            {
            }
        }

        result = default;
        return false;
    }

    private static string NormalizeText(string value)
    {
        return value
            .Replace("\u00A0", " ")
            .Replace("\u202F", " ")
            .Trim();
    }

    private static bool IsSummaryDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        var normalized = description
            .ToUpperInvariant()
            .Replace("İ", "I")
            .Replace("Ç", "C")
            .Replace("Ğ", "G")
            .Replace("Ö", "O")
            .Replace("Ş", "S")
            .Replace("Ü", "U");

        return normalized.Contains("TOPLAM")
            || normalized.Contains("ARA TOPLAM")
            || normalized.Contains("GENEL TOPLAM")
            || normalized.Contains("DEVIR")
            || normalized.Contains("BAKIYE");
    }

    private static bool IsValidDecimal182(decimal value)
    {
        return value <= MaxDecimal182 && GetScale(value) <= 2;
    }

    private static bool IsValidDecimal183(decimal value)
    {
        return value <= MaxDecimal183 && GetScale(value) <= 3;
    }

    private static int GetScale(decimal value)
    {
        return (decimal.GetBits(value)[3] >> 16) & 0xFF;
    }
}





