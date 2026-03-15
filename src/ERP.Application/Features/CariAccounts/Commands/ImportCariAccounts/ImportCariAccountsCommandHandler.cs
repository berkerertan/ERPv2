using ERP.Application.Abstractions.Imports;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Models;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using FluentValidation;
using MediatR;
using System.Globalization;

namespace ERP.Application.Features.CariAccounts.Commands.ImportCariAccounts;

public sealed class ImportCariAccountsCommandHandler(
    ICariAccountRepository repository,
    ICariAccountExcelReader excelReader)
    : IRequestHandler<ImportCariAccountsCommand, CariAccountImportResult>
{
    public async Task<CariAccountImportResult> Handle(ImportCariAccountsCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<CariAccountExcelRow> rows;

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

        if (rows.Count == 0)
        {
            return new CariAccountImportResult(0, 0, 0, 0, 0, Array.Empty<string>());
        }

        var errors = new List<string>();
        var rowCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var updated = 0;
        var skipped = 0;

        var defaultTypeText = request.ColumnMapping?.DefaultType;
        if (string.IsNullOrWhiteSpace(defaultTypeText))
        {
            defaultTypeText = nameof(CariType.BuyerBch);
        }

        var codePrefix = request.ColumnMapping?.CodePrefix;
        if (string.IsNullOrWhiteSpace(codePrefix))
        {
            codePrefix = "BCH";
        }

        foreach (var row in rows)
        {
            var name = row.Name.Trim();
            var code = row.Code.Trim();
            var phone = NormalizePhone(row.Phone);
            var typeText = row.Type.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Row {row.RowNumber}: Name is required.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                code = GenerateCode(codePrefix, row.RowNumber);
            }

            if (code.Length > 25)
            {
                errors.Add($"Row {row.RowNumber}: Code length exceeds 25 characters.");
                continue;
            }

            if (name.Length > 150)
            {
                errors.Add($"Row {row.RowNumber}: Name length exceeds 150 characters.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 30)
            {
                errors.Add($"Row {row.RowNumber}: Phone length exceeds 30 characters.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(phone) && !IsValidPhone(phone))
            {
                errors.Add($"Row {row.RowNumber}: Invalid phone format '{row.Phone}'.");
                continue;
            }

            if (!rowCodes.Add(code))
            {
                errors.Add($"Row {row.RowNumber}: Duplicate code '{code}' in file.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(typeText))
            {
                typeText = defaultTypeText;
            }

            if (!TryParseCariType(typeText, out var type))
            {
                errors.Add($"Row {row.RowNumber}: Invalid type '{row.Type}'. Use Supplier, BuyerBch/BCH, Both or 1/2/3.");
                continue;
            }

            if (!TryParseDecimal(row.RiskLimit, out var riskLimit))
            {
                errors.Add($"Row {row.RowNumber}: Invalid risk limit '{row.RiskLimit}'.");
                continue;
            }

            if (!TryParseInt(row.MaturityDays, out var maturityDays))
            {
                errors.Add($"Row {row.RowNumber}: Invalid maturity days '{row.MaturityDays}'.");
                continue;
            }

            if (riskLimit < 0)
            {
                errors.Add($"Row {row.RowNumber}: Risk limit cannot be negative.");
                continue;
            }

            if (riskLimit > 9999999999999999.99m)
            {
                errors.Add($"Row {row.RowNumber}: Risk limit exceeds database precision (decimal(18,2)).");
                continue;
            }

            if (maturityDays < 0 || maturityDays > 365)
            {
                errors.Add($"Row {row.RowNumber}: Maturity days must be between 0 and 365.");
                continue;
            }

            var existing = await repository.GetByCodeAsync(code, cancellationToken);

            if (existing is null)
            {
                await repository.AddAsync(new CariAccount
                {
                    Code = code,
                    Name = name,
                    Phone = phone,
                    Type = type,
                    RiskLimit = riskLimit,
                    MaturityDays = maturityDays,
                    CurrentBalance = 0m
                }, cancellationToken);

                created++;
                continue;
            }

            if (!request.UpsertExisting)
            {
                skipped++;
                continue;
            }

            existing.Name = name;
            existing.Phone = phone;
            existing.Type = type;
            existing.RiskLimit = riskLimit;
            existing.MaturityDays = maturityDays;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            await repository.UpdateAsync(existing, cancellationToken);
            updated++;
        }

        return new CariAccountImportResult(
            rows.Count,
            created,
            updated,
            skipped,
            errors.Count,
            errors);
    }

    private static string GenerateCode(string prefix, int rowNumber)
    {
        var normalizedPrefix = new string(prefix.Where(char.IsLetterOrDigit).ToArray());

        if (string.IsNullOrWhiteSpace(normalizedPrefix))
        {
            normalizedPrefix = "BCH";
        }

        var code = $"{normalizedPrefix}-{rowNumber:00000}";
        if (code.Length > 25)
        {
            code = code[..25];
        }

        return code;
    }

    private static bool TryParseCariType(string typeText, out CariType type)
    {
        type = default;

        if (string.IsNullOrWhiteSpace(typeText))
        {
            return false;
        }

        var normalized = Normalize(typeText);

        if (normalized == "1")
        {
            type = CariType.BuyerBch;
            return true;
        }

        if (normalized == "2")
        {
            type = CariType.Supplier;
            return true;
        }

        if (normalized == "3")
        {
            type = CariType.Both;
            return true;
        }

        if (normalized.Contains("TEDARIK") || normalized.Contains("SUPPLIER"))
        {
            type = CariType.Supplier;
            return true;
        }

        if (normalized.Contains("BCH") || normalized.Contains("ALICI") || normalized.Contains("BUYER") || normalized.Contains("BORCLU"))
        {
            type = CariType.BuyerBch;
            return true;
        }

        if (normalized.Contains("BOTH") || normalized.Contains("HERIKISI") || normalized.Contains("HEPSI"))
        {
            type = CariType.Both;
            return true;
        }

        if (Enum.TryParse<CariType>(typeText, ignoreCase: true, out var enumType))
        {
            type = enumType;
            return true;
        }

        return false;
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0m;
            return true;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out result);
    }

    private static bool TryParseInt(string value, out int result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0;
            return true;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
            || int.TryParse(value, NumberStyles.Integer, CultureInfo.GetCultureInfo("tr-TR"), out result);
    }

    private static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsValidPhone(string value)
    {
        foreach (var ch in value)
        {
            if (char.IsDigit(ch))
            {
                continue;
            }

            if (ch is '+' or '-' or '(' or ')' or ' ')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .ToUpperInvariant()
            .Replace("\u0130", "I")
            .Replace("\u00C7", "C")
            .Replace("\u011E", "G")
            .Replace("\u00D6", "O")
            .Replace("\u015E", "S")
            .Replace("\u00DC", "U")
            .Replace("\u0131", "I")
            .Replace("\u00E7", "C")
            .Replace("\u011F", "G")
            .Replace("\u00F6", "O")
            .Replace("\u015F", "S")
            .Replace("\u00FC", "U")
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .Replace(" ", string.Empty);
    }
}


